using System;
using System.Threading;
using System.Threading.Tasks;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// An <see cref="IDbContextProvider{TContext}"/> implementation that wraps the original one, throwing a <see cref="DbUpdateConcurrencyException"/> on the first attempt, at the end of the executed task.
	/// </para>
	/// <para>
	/// By executing all of the work before throwing, the most mistakes are detected (such as an auto-increment ID already being assigned when the next attempt begins).
	/// </para>
	/// </summary>
	public class ConcurrencyConflictDbContextProvider<TContext, TDbContext> : OverridingDbContextProvider<TContext, TDbContext>
		where TDbContext : DbContext
	{
		private object? LastSeenUnitOfWork { get; set; }

		public override DbContextScopeOptions Options => this.WrappedProvider.Options;

		private IDbContextProvider<TContext> WrappedProvider { get; }

		private bool AfterCommit { get; }

		/// <param name="afterCommit">If true, any ongoing transaction is committed before the exception occurs, simulating an exception on commit where the commit has actually succeeded.</param>
		public ConcurrencyConflictDbContextProvider(IDbContextProvider<TContext> wrappedProvider, bool afterCommit = false)
		{
			this.WrappedProvider = wrappedProvider ?? throw new ArgumentNullException(nameof(wrappedProvider));
			this.AfterCommit = afterCommit;
		}

		public override DbContextScope CreateDbContextScope(AmbientScopeOption? scopeOption = null)
		{
			return this.WrappedProvider.CreateDbContextScope(scopeOption);
		}

		protected override IExecutionStrategy GetExecutionStrategyFromDbContext(DbContext dbContext)
		{
			throw new NotSupportedException("This code should not be reached, since we delegate to the wrapped instance at a higher level.");
		}

		protected override TResult ExecuteInDbContextScope<TState, TResult>(AmbientScopeOption scopeOption, TState state, Func<IExecutionScope<TState>, TResult> task)
		{
			return this.WrappedProvider.ExecuteInDbContextScope(scopeOption, state, scope =>
			{
				var shouldThrow = this.ShouldThrow();
				var result = task(scope);
				ThrowConcurrencyException(shouldThrow);
				return result;
			});
		}

		protected override Task<TResult> ExecuteInDbContextScopeAsync<TState, TResult>(AmbientScopeOption scopeOption, TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			return this.WrappedProvider.ExecuteInDbContextScopeAsync(scopeOption, state, cancellationToken, async (scope, ct) =>
			{
				var shouldThrow = this.ShouldThrow();
				var result = await task(scope, ct);
				ThrowConcurrencyException(shouldThrow);
				return result;
			});
		}

		private bool ShouldThrow()
		{
			// We will only throw (at the end of the operation) if we see a unit of work for the first time
			var unitOfWork = DbContextScope<TDbContext>.Current.UnitOfWork;
			if (unitOfWork == this.LastSeenUnitOfWork) return false;

			this.LastSeenUnitOfWork = unitOfWork;

			return true;
		}

		private void ThrowConcurrencyException(bool shouldThrow)
		{
			if (shouldThrow)
			{
				if (this.AfterCommit && DbContextScope<TDbContext>.Current.DbContext.Database.CurrentTransaction != null)
					DbContextScope<TDbContext>.Current.DbContext.Database.CommitTransaction();

				throw new DbUpdateConcurrencyException("This is a simulated optimistic concurrency exception.");
			}
		}
	}
}
