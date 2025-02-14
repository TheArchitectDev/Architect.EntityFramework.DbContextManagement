using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement.DbContextScopes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// An <see cref="IDbContextProvider{TContext}"/> implementation that wraps the original one, throwing a <see cref="DbUpdateConcurrencyException"/> on the first attempt, at the end of the executed task.
	/// </para>
	/// <para>
	/// This helps test retry behavior, by providing a scenario where the second attempt should succeed.
	/// </para>
	/// <para>
	/// By executing all of the work before throwing, the most mistakes are detected (such as an auto-increment ID already being assigned when the next attempt begins).
	/// </para>
	/// </summary>
	public class ConcurrencyConflictDbContextProvider<TContext, TDbContext> : DbContextProvider<TContext, TDbContext>
		where TDbContext : DbContext
	{
		public override DbContextScopeOptions Options => this.WrappedProvider.Options;

		private ConditionalWeakTable<UnitOfWork, object> SeenUnitsOfWork { get; } = new ConditionalWeakTable<UnitOfWork, object>();

		private IDbContextProvider<TContext> WrappedProvider { get; }

		/// <summary>
		/// If true, any ongoing transaction is committed before the exception occurs, simulating an exception on commit where the commit has actually succeeded.
		/// </summary>
		private bool AfterCommit { get; }

		/// <summary>
		/// <para>
		/// Constructs an <see cref="IDbContextProvider{TContext}"/> implementation wrapping the given one.
		/// It throws a <see cref="DbUpdateConcurrencyException"/> on the first attempt of variants of
		/// <see cref="IDbContextProvider{TContext}.ExecuteInDbContextScope{TState, TResult}(AmbientScopeOption, TState, Func{IExecutionScope{TState}, TResult})"/>, at the end of the executed task.
		/// </para>
		/// <para>
		/// This helps test retry behavior, by providing a scenario where the second attempt should succeed.
		/// </para>
		/// <para>
		/// By executing all of the work before throwing, the most mistakes are detected (such as an auto-increment ID already being assigned when the next attempt begins).
		/// </para>
		/// </summary>
		/// <param name="wrappedProvider"></param>
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

		public override TResult ExecuteInDbContextScope<TState, TResult>(AmbientScopeOption scopeOption, TState state, Func<IExecutionScope<TState>, TResult> task)
		{
			return this.WrappedProvider.ExecuteInDbContextScope(scopeOption, state, scope =>
			{
				var shouldThrow = this.ShouldThrow();
				var result = task(scope);
				this.ThrowConcurrencyException(shouldThrow);
				return result;
			});
		}

		public override Task<TResult> ExecuteInDbContextScopeAsync<TState, TResult>(AmbientScopeOption scopeOption, TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			return this.WrappedProvider.ExecuteInDbContextScopeAsync(scopeOption, state, cancellationToken, async (scope, ct) =>
			{
				var shouldThrow = this.ShouldThrow();
				var result = await task(scope, ct).ConfigureAwait(false);
				this.ThrowConcurrencyException(shouldThrow);
				return result;
			});
		}

		private bool ShouldThrow()
		{
			// We will only throw if we see a unit of work for the first time
			var unitOfWork = DbContextScope<TDbContext>.Current.UnitOfWork;

			if (this.SeenUnitsOfWork.TryGetValue(unitOfWork, out _)) return false; // Already seen

			var ourKey = new object();
			var registeredKey = this.SeenUnitsOfWork.GetValue(unitOfWork, _ => ourKey);

			if (registeredKey != ourKey) return false; // Lost a race condition, so we are not the one that should throw

			return true;
		}

		private void ThrowConcurrencyException(bool shouldThrow)
		{
			if (shouldThrow)
			{
				if (this.AfterCommit && DbContextScope<TDbContext>.Current.DbContext.Database.CurrentTransaction is not null)
					DbContextScope<TDbContext>.Current.DbContext.Database.CommitTransaction();

				throw new DbUpdateConcurrencyException("This is a simulated optimistic concurrency exception.");
			}
		}
	}
}
