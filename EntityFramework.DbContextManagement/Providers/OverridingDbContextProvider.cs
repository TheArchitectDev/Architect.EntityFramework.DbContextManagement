using System;
using System.Threading;
using System.Threading.Tasks;
using Architect.AmbientContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Architect.EntityFramework.DbContextManagement.Providers
{
	/// <summary>
	/// Used to create a provider that overrides the interface's default implementations, without overlooking any essential ones.
	/// </summary>
	public abstract class OverridingDbContextProvider<TDbContext> : IDbContextProvider<TDbContext>
		where TDbContext : DbContext
	{
		DbContextScopeOptions IDbContextProvider<TDbContext>.Options => this.Options;
		public abstract DbContextScopeOptions Options { get; }

		public abstract DbContextScope CreateDbContextScope(AmbientScopeOption? scopeOption = null);

		IExecutionStrategy IDbContextProvider<TDbContext>.GetExecutionStrategyFromDbContext(DbContext dbContext)
		{
			return this.GetExecutionStrategyFromDbContext(dbContext);
		}
		protected abstract IExecutionStrategy GetExecutionStrategyFromDbContext(DbContext dbContext);

		TResult IDbContextProvider<TDbContext>.ExecuteInDbContextScope<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, Func<IExecutionScope<TState>, TResult> task)
		{
			return this.ExecuteInDbContextScope(scopeOption, state, task);
		}
		protected abstract TResult ExecuteInDbContextScope<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, Func<IExecutionScope<TState>, TResult> task);

		Task<TResult> IDbContextProvider<TDbContext>.ExecuteInDbContextScopeAsync<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			return this.ExecuteInDbContextScopeAsync(scopeOption, state, cancellationToken, task);
		}
		protected abstract Task<TResult> ExecuteInDbContextScopeAsync<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task);
	}

	/// <summary>
	/// Used to create a provider that overrides the interface's default implementations, without overlooking any essential ones.
	/// </summary>
	public abstract class OverridingDbContextProvider<TContext, TDbContext> : OverridingDbContextProvider<TDbContext>, IDbContextProvider<TContext>
		where TDbContext : DbContext
	{
		DbContextScopeOptions IDbContextProvider<TContext>.Options => this.Options;

		IExecutionStrategy IDbContextProvider<TContext>.GetExecutionStrategyFromDbContext(DbContext dbContext)
		{
			return this.GetExecutionStrategyFromDbContext(dbContext);
		}

		TResult IDbContextProvider<TContext>.ExecuteInDbContextScope<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, Func<IExecutionScope<TState>, TResult> task)
		{
			return this.ExecuteInDbContextScope(scopeOption, state, task);
		}

		Task<TResult> IDbContextProvider<TContext>.ExecuteInDbContextScopeAsync<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			return this.ExecuteInDbContextScopeAsync(scopeOption, state, cancellationToken, task);
		}
	}
}
