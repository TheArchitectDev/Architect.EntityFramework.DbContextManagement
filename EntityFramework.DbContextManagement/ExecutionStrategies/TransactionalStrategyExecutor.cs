using System;
using System.Threading;
using System.Threading.Tasks;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement.DbContextScopes;
using Architect.EntityFramework.DbContextManagement.ExecutionScopes;
using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.ExecutionStrategies
{
	internal static class TransactionalStrategyExecutor
	{
		public static TResult ExecuteInDbContextScope<TContext, TState, TResult>(this IDbContextProvider<TContext> provider,
			AmbientScopeOption scopeOption,
			TState state, Func<IExecutionScope<TState>, TResult> task)
		{
			return ExecuteInDbContextScopeAsync(provider, scopeOption, state, cancellationToken: default, ExecuteSynchronously, async: false, GetUnitOfWorkFromDbContextScope)
				.AssumeSynchronous();

			// Local function that executes the given task and returns a completed task
			Task<TResult> ExecuteSynchronously(IExecutionScope<TState> executionScope, CancellationToken _)
			{
				var result = task(executionScope);
				return Task.FromResult(result);
			}
		}

		public static Task<TResult> ExecuteInDbContextScopeAsync<TContext, TState, TResult>(this IDbContextProvider<TContext> provider,
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			return ExecuteInDbContextScopeAsync(provider, scopeOption, state, cancellationToken, task, async: true, GetUnitOfWorkFromDbContextScope);
		}

		private static UnitOfWork GetUnitOfWorkFromDbContextScope(DbContextScope dbContextScope)
		{
			return dbContextScope.UnitOfWork;
		}

		// #TODO: Remove state?
		/// <param name="async">If false, then the given <paramref name="task"/> MUST complete synchronously.</param>
		internal static async Task<TResult> ExecuteInDbContextScopeAsync<TContext, TState, TResult>(this IDbContextProvider<TContext> provider,
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task,
			bool async, Func<DbContextScope, UnitOfWork> getUnitOfWork, bool shouldClearChangeTrackerOnRetry = true)
		{
			// #TODO: Test if we should hide Transaction.Current if there is one!!

			if (provider is null) throw new ArgumentNullException(nameof(provider));

			// "Note that any contexts should be constructed within the code block to be retried. This ensures that we are starting with a clean state for each retry."
			// https://docs.microsoft.com/en-us/ef/ef6/fundamentals/connection-resiliency/retry-logic
			// However, since EF Core 5, we have confirmation that we may reuse the DbContext, and we should call DbContext.ChangeTracker.Clear() before each retry:
			// https://github.com/dotnet/efcore/discussions/22422#discussioncomment-84480
			var dbContextScope = provider.CreateDbContextScope(scopeOption);

			var unitOfWork = getUnitOfWork(dbContextScope);

			try
			{
				dbContextScope.ScopeType = DbContextScopeType.ScopedExecution;

				if (!dbContextScope.IsRootScope)
				{
					ThrowIfParentScopeIsNotForScopedExecution(dbContextScope);

					var executionScope = new ExecutionScope<TState>(unitOfWork, dbContextScope.DbContext, isNested: true, state);

					// Simply execute the work as part of the overarching work
					try
					{
						return await task(executionScope, cancellationToken);
					}
					catch
					{
						executionScope.Abort();
						throw;
					}
					finally
					{
						if (async) await executionScope.DisposeAsync();
						else executionScope.Dispose();
					}
				}

				_ = unitOfWork.DbContextObserver; // Ensure that the observer is resolved

				var executionStrategy = provider.CreateExecutionStrategy(dbContextScope.DbContext);

				return async
					? await executionStrategy.ExecuteAsync(ExecuteAsync, cancellationToken)
					: executionStrategy.Execute(() => ExecuteAsync(cancellationToken).AssumeSynchronous());
			}
			finally
			{
				if (async) await dbContextScope.DisposeAsync();
				else dbContextScope.Dispose();
			}

			// Local function that performs the input task with a clean DbContext
			async Task<TResult> ExecuteAsync(CancellationToken cancellationToken)
			{
				// Note: When pooling is disabled, if a DbContext is disposed prematurely (not by its owner), things can always go horribly wrong
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
						// #TODO: Test retries with SQL Server
						result = await task(executionScope, cancellationToken);

						isExecutionScopeCompleted = executionScope.IsCompleted;
					}
					catch
					{
						executionScope.Abort();
						throw;
					}
					finally
					{
						if (async) await executionScope.DisposeAsync();
						else executionScope.Dispose();
					}

					// If we were completed, commit the ongoing transaction, if any
					if (isExecutionScopeCompleted)
					{
						try
						{
							await unitOfWork.TryCommitTransactionAsync(async, cancellationToken);
						}
						catch (Exception e) when (provider.Options.AvoidFailureOnCommitRetries) // #TODO: Test
						{
							throw new Exception("The operation failed on commit. Since it is possible that the commit succeeded, potential retries were avoided.", e);
						}
					}

					return result;
				}
				catch
				{
					// Reset the DbContext so that potential retries start fresh
					// We created the DbContext (because this method is only invoked with a root DbContextScope), so we are free to clear it
					if (shouldClearChangeTrackerOnRetry)
						dbContext.ChangeTracker.Clear();

					// Allow reuse
					unitOfWork.UndoInvalidation();

					throw;
				}
			}
		}

		/// <summary>
		/// Throws an exception if the given <see cref="DbContextScope"/>'s parent has a scope type other than <see cref="DbContextScopeType.ScopedExecution"/>.
		/// </summary>
		public static void ThrowIfParentScopeIsNotForScopedExecution(DbContextScope dbContextScope)
		{
			if (dbContextScope.ParentScopeType != DbContextScopeType.ScopedExecution)
			{
				throw new InvalidOperationException($"Scoped execution inside a parent scope that does not use scoped execution results in undefined behavior. To avoid this exception, use methods like {nameof(IDbContextProvider<object>)}.{nameof(IDbContextProvider<object>.ExecuteInDbContextScope)}() for the outer behavior as well (recommended). Alternatively, use manual operation of the {nameof(DbContextScope)} everywhere.");
			}
		}
	}
}
