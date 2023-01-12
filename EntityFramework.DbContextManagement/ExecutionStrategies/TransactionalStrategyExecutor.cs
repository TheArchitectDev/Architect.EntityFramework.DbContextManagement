using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement.DbContextScopes;
using Architect.EntityFramework.DbContextManagement.ExecutionScopes;
using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.ExecutionStrategies
{
	internal static class TransactionalStrategyExecutor
	{
		private static UnitOfWork GetUnitOfWorkFromDbContextScope(DbContextScope dbContextScope)
		{
			return dbContextScope.UnitOfWork;
		}

		public static TResult ExecuteInDbContextScope<TContext, TState, TResult>(IDbContextProvider<TContext> provider,
			AmbientScopeOption scopeOption,
			TState state, Func<IExecutionScope<TState>, TResult> task)
		{
			return ExecuteInDbContextScopeAsync(provider, scopeOption, state, cancellationToken: default, ExecuteSynchronously, async: false, GetUnitOfWorkFromDbContextScope)
				.RequireCompleted();

			// Local function that executes the given task and returns a completed task
			Task<TResult> ExecuteSynchronously(IExecutionScope<TState> executionScope, CancellationToken _)
			{
				var result = task(executionScope);
				return Task.FromResult(result);
			}
		}

		public static Task<TResult> ExecuteInDbContextScopeAsync<TContext, TState, TResult>(IDbContextProvider<TContext> provider,
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			return ExecuteInDbContextScopeAsync(provider, scopeOption, state, cancellationToken, task, async: true, GetUnitOfWorkFromDbContextScope);
		}

		/// <param name="async">If false, then the given <paramref name="task"/> MUST complete synchronously.</param>
		internal static async Task<TResult> ExecuteInDbContextScopeAsync<TContext, TState, TResult>(IDbContextProvider<TContext> provider,
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task,
			bool async, Func<DbContextScope, UnitOfWork> getUnitOfWork, bool shouldClearChangeTrackerOnRetry = true)
		{
			if (provider is null) throw new ArgumentNullException(nameof(provider));

			// TODO Consideration: We could hide the ambient transaction if there is one
			if (Transaction.Current is not null)
				throw new InvalidOperationException("An ambient TransactionScope has been detected. DbContextScope was nog designed to support TransactionScopes.");

			// "Note that any contexts should be constructed within the code block to be retried. This ensures that we are starting with a clean state for each retry."
			// https://docs.microsoft.com/en-us/ef/ef6/fundamentals/connection-resiliency/retry-logic
			// However, since EF Core 5, we have confirmation that we may reuse the DbContext, and we should call DbContext.ChangeTracker.Clear() (and close the connection, because of session state) before each retry:
			// https://github.com/dotnet/efcore/discussions/22422#discussioncomment-84480
			var dbContextScope = provider.CreateDbContextScope(scopeOption);

			try
			{
				var unitOfWork = getUnitOfWork(dbContextScope);

				if (dbContextScope.IsNested)
					return await PerformAsPartOfOuterScope(async, dbContextScope, unitOfWork, state, cancellationToken, task).ConfigureAwait(false);

				unitOfWork.PromoteToMode(UnitOfWorkMode.ScopedExecution);

				_ = unitOfWork.DbContextObserver; // Ensure that the observer is resolved

				var executionStrategy = provider.CreateExecutionStrategy(dbContextScope.DbContext);

				return async
					? await executionStrategy.ExecuteAsync(
						ct => PerformScoped(async, shouldClearChangeTrackerOnRetry, dbContextScope, unitOfWork, state, ct, task), cancellationToken).ConfigureAwait(false)
					: executionStrategy.Execute(
						() => PerformScoped(async, shouldClearChangeTrackerOnRetry, dbContextScope, unitOfWork, state, cancellationToken: default, task).RequireCompleted());
			}
			finally
			{
				if (async) await dbContextScope.DisposeAsync().ConfigureAwait(false);
				else dbContextScope.Dispose();
			}
		}

		/// <param name="async">If false, then the given <paramref name="task"/> MUST complete synchronously.</param>
		private static async Task<TResult> PerformAsPartOfOuterScope<TState, TResult>(bool async,
			DbContextScope dbContextScope, UnitOfWork unitOfWork,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			ThrowIfUnitOfWorkIsNotSetForScopedExecution(unitOfWork);

			var executionScope = new ExecutionScope<TState>(unitOfWork, dbContextScope.DbContext, isNested: true, state);

			// Simply execute the work as part of the overarching work
			try
			{
				return await task(executionScope, cancellationToken).ConfigureAwait(false);
			}
			catch
			{
				executionScope.Abort();
				throw;
			}
			finally
			{
				if (async) await executionScope.DisposeAsync().ConfigureAwait(false);
				else executionScope.Dispose();
			}
		}

		private static async Task<TResult> PerformScoped<TState, TResult>(bool async, bool shouldClearChangeTrackerOnRetry,
			DbContextScope dbContextScope, UnitOfWork unitOfWork,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			// Note: When pooling is enabled, if a DbContext is disposed prematurely (not by its owner), things can always go horribly wrong
			// It is no different for this method
			// We could protect against this by checking the lease count, but it is extreme developer error and it can still go wrong in the developer's own code

			var dbContext = dbContextScope.DbContext;
			try
			{
				TResult result;

				bool isExecutionScopeCompleted;
				var executionScope = new ExecutionScope<TState>(unitOfWork, dbContext, isNested: true, state);

				try
				{
					result = await task(executionScope, cancellationToken).ConfigureAwait(false);

					isExecutionScopeCompleted = executionScope.IsCompleted;
				}
				catch
				{
					executionScope.Abort();
					throw;
				}
				finally
				{
					if (async) await executionScope.DisposeAsync().ConfigureAwait(false);
					else executionScope.Dispose();
				}

				// If we were completed, commit the ongoing transaction, if any
				if (isExecutionScopeCompleted)
					await unitOfWork.TryCommitTransactionAsync(async, cancellationToken).ConfigureAwait(false);

				return result;
			}
			catch
			{
				// Reset the DbContext so that potential retries start fresh
				// We created the DbContext (because this method is only invoked with a root DbContextScope), so we are free to clear it
				if (shouldClearChangeTrackerOnRetry)
				{
					dbContext.ChangeTracker.Clear();
					await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
				}

				// Allow reuse
				unitOfWork.UndoInvalidation();

				throw;
			}
		}

		/// <summary>
		/// Throws an exception if the given <see cref="UnitOfWork"/> is not set for <see cref="UnitOfWorkMode.ScopedExecution"/>.
		/// </summary>
		public static void ThrowIfUnitOfWorkIsNotSetForScopedExecution(UnitOfWork unitOfWork)
		{
			if (unitOfWork.Mode != UnitOfWorkMode.ScopedExecution)
			{
				throw new InvalidOperationException($"Scoped execution inside a parent scope that does not use scoped execution results in undefined behavior. To avoid this exception, use methods like {nameof(IDbContextProvider<object>)}.{nameof(IDbContextProvider<object>.ExecuteInDbContextScope)}() for the outer behavior as well (recommended). Alternatively, use manual operation of the {nameof(DbContextScope)} everywhere.");
			}
		}
	}
}
