using System;
using System.Threading.Tasks;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement.DbContextScopes;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// Provides access to a <see cref="Microsoft.EntityFrameworkCore.DbContext"/> through the Ambient Context pattern.
	/// </para>
	/// <para>
	/// Instances are usually provided through <see cref="IDbContextProvider{TContext}"/> and accessed through <see cref="IDbContextAccessor{TDbContext}"/>.
	/// </para>
	/// </summary>
	public abstract class DbContextScope : AsyncAmbientScope<DbContextScope>
	{
		/// <summary>
		/// Creates a new ambiently accessible instance.
		/// It should be disposed with the help of a using statement.
		/// </summary>
		internal static DbContextScope<TDbContext> Create<TDbContext>(
			IDbContextFactory<TDbContext> dbContextFactory,
			DbContextScopeOptions options,
			AmbientScopeOption scopeOption)
			where TDbContext : DbContext
		{
			return Create(dbContextFactory.CreateDbContext, options, scopeOption);
		}

		/// <summary>
		/// Creates a new ambiently accessible instance.
		/// It should be disposed with the help of a using statement.
		/// </summary>
		internal static DbContextScope<TDbContext> Create<TDbContext>(
			Func<TDbContext> dbContextFactory,
			DbContextScopeOptions options,
			AmbientScopeOption scopeOption)
			where TDbContext : DbContext
		{
			return new DbContextScope<TDbContext>(dbContextFactory, options, scopeOption);
		}

		/// <summary>
		/// The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> available in the current scope.
		/// It is shared with any joined parent and child scopes.
		/// </summary>
		public abstract DbContext DbContext { get; }
		internal abstract UnitOfWork UnitOfWork { get; }

		/// <summary>
		/// Allows the scope to be of a particular type.
		/// Scopes of different types may or may not be compatible.
		/// </summary>
		internal DbContextScopeType ScopeType { get; set; }

		/// <summary>
		/// True if the current scope is an effective root scope, i.e. it has not joined any parent scope.
		/// </summary>
		public bool IsRootScope => this.EffectiveParentScope is null;

		/// <summary>
		/// The <see cref="ScopeType"/> of the parent, or null if there is no parent.
		/// </summary>
		internal DbContextScopeType? ParentScopeType => this.EffectiveParentScope?.ScopeType;

		private protected DbContextScope(AmbientScopeOption scopeOption)
			: base(scopeOption)
		{
		}
	}

	/// <summary>
	/// An intermediate type between <see cref="DbContextScope"/> and <see cref="DbContextScope{TDbContext}"/>.
	/// Allows properties such as <see cref="DbContextScope.DbContext"/> to be both overridden and replaced by more specifically-typed ones.
	/// </summary>
	/// <typeparam name="TDbContext"></typeparam>
	public abstract class TypedDbContextScope<TDbContext> : DbContextScope
		where TDbContext : DbContext
	{
		// By overriding in an intermediate class we can 
		public sealed override DbContext DbContext => (this as DbContextScope<TDbContext>)!.DbContext;
		internal sealed override UnitOfWork UnitOfWork => (this as DbContextScope<TDbContext>)!.UnitOfWork;

		/// <summary>
		/// Only accessible within this package.
		/// Will only be invoked by <see cref="DbContextScope{TDbContext}"/>.
		/// </summary>
		private protected TypedDbContextScope(AmbientScopeOption scopeOption)
			: base(scopeOption)
		{
			System.Diagnostics.Debug.Assert(this is DbContextScope<TDbContext>);
		}
	}

	/// <summary>
	/// <para>
	/// Provides access to a <typeparamref name="TDbContext"/> through the Ambient Context pattern.
	/// </para>
	/// <para>
	/// Instances are usually provided through <see cref="IDbContextProvider{TContext}"/> and accessed through <see cref="IDbContextAccessor{TDbContext}"/>.
	/// </para>
	/// </summary>
	public sealed class DbContextScope<TDbContext> : TypedDbContextScope<TDbContext>
		where TDbContext : DbContext
	{
		/// <summary>
		/// Returns the current ambient <see cref="DbContextScope{TDbContext}"/>, or null if there is none.
		/// </summary>
		internal static DbContextScope<TDbContext>? CurrentOrDefault => GetAmbientScope(considerDefaultScope: false) as DbContextScope<TDbContext>;

		/// <summary>
		/// Returns the current ambient <see cref="DbContextScope{TDbContext}"/>, or throws an <see cref="InvalidOperationException"/> if there is none.
		/// </summary>
		public static DbContextScope<TDbContext> Current => GetAmbientScope(considerDefaultScope: false) as DbContextScope<TDbContext> ??
			throw new InvalidOperationException("No ambient DbContext was registered.");

		/// <summary>
		/// Returns the current ambient <typeparamref name="TDbContext"/>, or throws an <see cref="InvalidOperationException"/> if there is none.
		/// </summary>
		public static TDbContext CurrentDbContext => Current.DbContext;

		private new DbContextScope<TDbContext> EffectiveParentScope => (base.EffectiveParentScope as DbContextScope<TDbContext>)!;

		/// <summary>
		/// The <typeparamref name="TDbContext"/> available in the current scope.
		/// It is shared with any joined parent and child scopes.
		/// </summary>
		public new TDbContext DbContext => this.UnitOfWork.DbContext;

		/// <summary>
		/// <para>
		/// The unit of work available in the current scope.
		/// It is shared with any joined parent and child scopes.
		/// </para>
		/// </summary>
		internal new UnitOfWork<TDbContext> UnitOfWork { get; private set; }

		internal DbContextScope(
			Func<TDbContext> dbContextFactory,
			DbContextScopeOptions options,
			AmbientScopeOption scopeOption)
			: base(scopeOption)
		{
			// Activate, gaining access to the potential parent scope
			this.Activate();

			try
			{
				this.UnitOfWork = this.EffectiveParentScope?.UnitOfWork ?? new UnitOfWork<TDbContext>(dbContextFactory, options);
			}
			catch
			{
				// Dispose ourselves on failure, ensuring that we are deactivated
				this.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Will only be invoked once, by the parent implementation.
		/// </summary>
		protected override void DisposeImplementation()
		{
			// If we are an effective root, we must dispose our UnitOfWork
			if (this.EffectiveParentScope is null && this.UnitOfWork != null)
				this.UnitOfWork.Dispose();
		}

		/// <summary>
		/// Will only be invoked once, by the parent implementation.
		/// </summary>
		protected override ValueTask DisposeAsyncImplementation()
		{
			// If we are an effective root, we must dispose our UnitOfWork
			if (this.EffectiveParentScope is null && this.UnitOfWork != null)
				return this.UnitOfWork.DisposeAsync();

			return new ValueTask(Task.CompletedTask);
		}
	}
}
