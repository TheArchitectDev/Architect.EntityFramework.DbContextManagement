using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Architect.AmbientContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// Provides an ambient <see cref="DbContextScope"/>, code in whose scope can access the <see cref="DbContext"/> through <see cref="IDbContextAccessor{TDbContext}"/>.
	/// </para>
	/// <para>
	/// <typeparamref name="TDbContext"/> may be a <see cref="DbContext"/> type, or a type used to represent one.
	/// Such an indirect representation can be registered with <see cref="DbContextScopeExtensions.AddDbContextScope{TDbContextRepresentation, TDbContext}
	/// (Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{DbContextScopeExtensions.Options{TDbContext}}?)"/>.
	/// </para>
	/// <para>
	/// Consider inheriting from <see cref="DbContextProvider{TDbContext}"/> if not implementing all methods.
	/// </para>
	/// </summary>
	public interface IDbContextProvider<TDbContext>
	{
		/// <summary>
		/// Returns the configuration for the current object.
		/// </summary>
		DbContextScopeOptions Options { get; }

		/// <summary>
		/// <para>
		/// Returns a new <see cref="DbContextScope"/>, setting it as the ambient one until it is disposed.
		/// </para>
		/// </summary>
		/// <param name="scopeOption">Controls the behavior with regards to potential outer scopes.</param>
		DbContextScope CreateDbContextScope(AmbientScopeOption? scopeOption = null);

		/// <summary>
		/// Returns the a new <see cref="IExecutionStrategy"/> determined by the given <see cref="DbContext"/> and the current provider.
		/// </summary>
		IExecutionStrategy CreateExecutionStrategy(DbContext dbContext);

		/// <summary>
		/// <para>
		/// Performs the given <paramref name="task"/>, with access to a new ambient <typeparamref name="TDbContext"/> accessible through <see cref="IDbContextAccessor{TDbContext}"/>.
		/// </para>
		/// <para>
		/// This is the preferred way to perform work in the scope of a <see cref="DbContext"/>. It takes care of many concerns automatically.
		/// </para>
		/// <para>
		/// The task is performed through the <see cref="DbContext"/>'s <see cref="IExecutionStrategy"/>.
		/// The <see cref="IExecutionStrategy"/> may provide behavior such as retry attempts on certain exceptions.
		/// Each attempt is provided with a fresh <see cref="DbContext"/>, with no state leakage.
		/// </para>
		/// <para>
		/// If a query is executed that might perform a write operation, a transaction is started automatically.
		/// (This comes at no additional cost, since otherwise Entity Framework starts its own transaction when saving.)
		/// The transaction is committed once the scope ends, provided that it has not aborted.
		/// </para>
		/// <para>
		/// Scopes can be nested. When a scope joins an outer scope, its work is simply performed as part of the outer scope's work, with the outer scope taking care of all the above.
		/// </para>
		/// <para>
		/// A scope aborts when an exception bubbles up from its task or when <see cref="IExecutionScope.Abort"/> is called. At the end of an aborted scope, any ongoing transaction is rolled back.
		/// Further attempts to use that <see cref="DbContext"/>, even by joined parent scopes, result in a <see cref="TransactionAbortedException"/>.
		/// </para>
		/// </summary>
		public TResult ExecuteInDbContextScope<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, Func<IExecutionScope<TState>, TResult> task);

		/// <summary>
		/// <para>
		/// Performs the given <paramref name="task"/>, with access to a new ambient <typeparamref name="TDbContext"/> accessible through <see cref="IDbContextAccessor{TDbContext}"/>.
		/// </para>
		/// <para>
		/// This is the preferred way to perform work in the scope of a <see cref="DbContext"/>. It takes care of many concerns automatically.
		/// </para>
		/// <para>
		/// The task is performed through the <see cref="DbContext"/>'s <see cref="IExecutionStrategy"/>.
		/// The <see cref="IExecutionStrategy"/> may provide behavior such as retry attempts on certain exceptions.
		/// Each attempt is provided with a fresh <see cref="DbContext"/>, with no state leakage.
		/// </para>
		/// <para>
		/// If a query is executed that might perform a write operation, a transaction is started automatically.
		/// (This comes at no additional cost, since otherwise Entity Framework starts its own transaction when saving.)
		/// The transaction is committed once the scope ends, provided that it has not aborted.
		/// </para>
		/// <para>
		/// Scopes can be nested. When a scope joins an outer scope, its work is simply performed as part of the outer scope's work, with the outer scope taking care of all the above.
		/// </para>
		/// <para>
		/// A scope aborts when an exception bubbles up from its task or when <see cref="IExecutionScope.Abort"/> is called. At the end of an aborted scope, any ongoing transaction is rolled back.
		/// Further attempts to use that <see cref="DbContext"/>, even by joined parent scopes, result in a <see cref="TransactionAbortedException"/>.
		/// </para>
		/// </summary>
		Task<TResult> ExecuteInDbContextScopeAsync<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task);
	}
}
