using System;
using System.Threading;
using System.Threading.Tasks;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement.ExecutionStrategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// Abstract base class for <see cref="IDbContextProvider{TDbContext}"/> for use when implementing a subset of the interface methods.
	/// </summary>
	/// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
	public abstract class DbContextProvider<TDbContext> : IDbContextProvider<TDbContext>
		where TDbContext : DbContext
	{
		public virtual DbContextScopeOptions Options => DbContextScopeOptions.Default;

		public abstract DbContextScope CreateDbContextScope(AmbientScopeOption? scopeOption = null);

		public IExecutionStrategy CreateExecutionStrategy(DbContext dbContext)
		{
			var executionStrategy = this.GetExecutionStrategyFromDbContext(dbContext);
			executionStrategy = this.CreateWrappingExecutionStrategy(executionStrategy);
			return executionStrategy;
		}

		/// <summary>
		/// Gets a new <see cref="IExecutionStrategy"/> directly from the given <see cref="DbContext"/>.
		/// </summary>
		protected virtual IExecutionStrategy GetExecutionStrategyFromDbContext(DbContext dbContext)
		{
			var executionStrategy = dbContext.Database.CreateExecutionStrategy();
			return executionStrategy;
		}

		/// <summary>
		/// Returns the given <see cref="IExecutionStrategy"/>, potentially wrapped in another strategy, depending on the <see cref="Options"/>.
		/// </summary>
		protected virtual IExecutionStrategy CreateWrappingExecutionStrategy(IExecutionStrategy baseExecutionStrategy)
		{
			if ((this.Options.ExecutionStrategyOptions & ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure) == ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure)
				baseExecutionStrategy = new RetryOnOptimisticConcurrencyFailureExecutionStrategy(baseExecutionStrategy);

			return baseExecutionStrategy;
		}

		public virtual TResult ExecuteInDbContextScope<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, Func<IExecutionScope<TState>, TResult> task)
		{
			return TransactionalStrategyExecutor.ExecuteInDbContextScope(this, scopeOption, state, task);
		}

		public virtual Task<TResult> ExecuteInDbContextScopeAsync<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			return TransactionalStrategyExecutor.ExecuteInDbContextScopeAsync(this, scopeOption, state, cancellationToken, task);
		}
	}

	/// <summary>
	/// Abstract base class for <see cref="IDbContextProvider{TDbContext}"/> for use when implementing a subset of the interface methods.
	/// </summary>
	/// <typeparam name="TContext">A type used merely to <strong>represent</strong> the <see cref="DbContext"/>, without needing an actual reference to <see cref="Microsoft.EntityFrameworkCore"/>.</typeparam>
	/// <typeparam name="TDbContext">The type of the actual <see cref="DbContext"/>.</typeparam>
	public abstract class DbContextProvider<TContext, TDbContext> : DbContextProvider<TDbContext>, IDbContextProvider<TContext>
		where TDbContext : DbContext
	{
	}
}
