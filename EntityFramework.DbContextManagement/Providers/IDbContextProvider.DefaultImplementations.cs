using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement.ExecutionStrategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	public partial interface IDbContextProvider<TContext>
	{
		// #TODO: Perhaps we should abstract all this away in a class, so that we can use virtual methods instead of all this trouble

		public DbContextScopeOptions Options => DbContextScopeOptions.Default;

		internal IExecutionStrategy CreateExecutionStrategy(DbContext dbContext)
		{
			var executionStrategy = this.GetExecutionStrategyFromDbContext(dbContext);
			executionStrategy = this.CreateWrappingExecutionStrategy(executionStrategy);
			return executionStrategy;
		}

		/// <summary>
		/// Gets a new <see cref="IExecutionStrategy"/> from the given <see cref="DbContext"/>.
		/// </summary>
		protected IExecutionStrategy GetExecutionStrategyFromDbContext(DbContext dbContext)
		{
			var executionStrategy = dbContext.Database.CreateExecutionStrategy();
			return executionStrategy;
		}

		/// <summary>
		/// Returns the given <see cref="IExecutionStrategy"/>, potentially wrapped in another strategy.
		/// </summary>
		protected IExecutionStrategy CreateWrappingExecutionStrategy(IExecutionStrategy baseExecutionStrategy)
		{
			if ((this.Options.ExecutionStrategyOptions & ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure) == ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure)
				baseExecutionStrategy = new RetryOnOptimisticConcurrencyFailureExecutionStrategy(baseExecutionStrategy);

			return baseExecutionStrategy;
		}

		// #TODO: Summary (once the main one is complete)
		public TResult ExecuteInDbContextScope<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, Func<IExecutionScope<TState>, TResult> task)
		{
			return TransactionalStrategyExecutor.ExecuteInDbContextScope(this, scopeOption, state, task);
		}

		/// <summary>
		/// <para>
		/// Performs the given <paramref name="task"/>, with access to a new ambient <typeparamref name="TContext"/> accessible through <see cref="IDbContextAccessor{TDbContext}"/>.
		/// </para>
		/// <para>
		/// If an outer scope is joined, the given task is simply executed as part of that scope's work.
		/// </para>
		/// <para>
		/// If the current scope is the outer scope, the task is performed through the <see cref="DbContext"/>'s <see cref="IExecutionStrategy"/>.
		/// The <see cref="IExecutionStrategy"/> may provide behavior such as retry attempts on certain exceptions.
		/// Each attempt is provided with a fresh <see cref="DbContext"/>, with no state leakage.
		/// </para>
		/// <para>
		/// If a query is executed that might perform a write operation, a transaction is started automatically.
		/// (This comes at no additional cost, since otherwise Entity Framework starts its own transaction when saving.)
		/// Once the <strong>outermost</strong> joined scope ends, if no scope has aborted, the transaction is committed.
		/// </para>
		/// <para>
		/// A scope aborts when an exception bubbles up from its task or when <see cref="IExecutionScope.Abort"/> is called. At the end of an aborted scope, any ongoing transaction is rolled back.
		/// Further attempts to use the <see cref="DbContext"/> by any joined scope result in a <see cref="TransactionAbortedException"/>.
		/// </para>
		/// </summary>
		public Task<TResult> ExecuteInDbContextScopeAsync<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			return TransactionalStrategyExecutor.ExecuteInDbContextScopeAsync(this, scopeOption, state, cancellationToken, task);
		}
	}
}
