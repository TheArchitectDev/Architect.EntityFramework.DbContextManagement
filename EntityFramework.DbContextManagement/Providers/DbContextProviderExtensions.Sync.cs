using System;
using System.Transactions;
using Architect.AmbientContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.EntityFramework.DbContextManagement
{
	public static partial class DbContextProviderExtensions
	{
		#region With state

		// This primary overload is specified on the interface itself
		//public static TResult ExecuteInDbContextScope<TDbContext, TState, TResult>(this IDbContextProvider<TDbContext> provider,
		//	AmbientScopeOption scopeOption,
		//	TState state, Func<IExecutionScope<TState>, TResult> task)
		//{
		//	return provider.ExecuteInDbContextScope(scopeOption, state, task);
		//}

		/// <summary>
		/// <para>
		/// Performs the given <paramref name="task"/>, with access to a new ambient <typeparamref name="TDbContext"/> accessible through <see cref="IDbContextAccessor{TDbContext}"/>.
		/// </para>
		/// <para>
		/// This is the preferred way to perform work in the scope of a <see cref="DbContext"/>. It takes care of many concerns automatically.
		/// </para>
		/// <para>
		/// The task is performed through the <see cref="DbContext"/>'s <see cref="IExecutionStrategy"/>.
		/// The <see cref="IExecutionStrategy"/> may provide behavior such as retry attempts on transient exceptions.
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
		internal static void ExecuteInDbContextScope<TDbContext, TState>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			TState state, Action<IExecutionScope<TState>> task)
		{
			provider.ExecuteInDbContextScope(scopeOption, state, ExecuteAndReturnTrue);

			// Local function that executes the given task and returns true
			bool ExecuteAndReturnTrue(IExecutionScope<TState> executionScope)
			{
				task(executionScope);
				return true;
			}
		}

		/// <summary>
		/// <para>
		/// Performs the given <paramref name="task"/>, with access to a new ambient <typeparamref name="TDbContext"/> accessible through <see cref="IDbContextAccessor{TDbContext}"/>.
		/// </para>
		/// <para>
		/// This is the preferred way to perform work in the scope of a <see cref="DbContext"/>. It takes care of many concerns automatically.
		/// </para>
		/// <para>
		/// The task is performed through the <see cref="DbContext"/>'s <see cref="IExecutionStrategy"/>.
		/// The <see cref="IExecutionStrategy"/> may provide behavior such as retry attempts on transient exceptions.
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
		public static TResult ExecuteInDbContextScope<TDbContext, TState, TResult>(this IDbContextProvider<TDbContext> provider,
			TState state, Func<IExecutionScope<TState>, TResult> task)
		{
			return provider.ExecuteInDbContextScope(provider.Options.DefaultScopeOption, state, task);
		}

		/// <summary>
		/// <para>
		/// Performs the given <paramref name="task"/>, with access to a new ambient <typeparamref name="TDbContext"/> accessible through <see cref="IDbContextAccessor{TDbContext}"/>.
		/// </para>
		/// <para>
		/// This is the preferred way to perform work in the scope of a <see cref="DbContext"/>. It takes care of many concerns automatically.
		/// </para>
		/// <para>
		/// The task is performed through the <see cref="DbContext"/>'s <see cref="IExecutionStrategy"/>.
		/// The <see cref="IExecutionStrategy"/> may provide behavior such as retry attempts on transient exceptions.
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
		public static void ExecuteInDbContextScope<TDbContext, TState>(this IDbContextProvider<TDbContext> provider,
			TState state, Action<IExecutionScope<TState>> task)
		{
			provider.ExecuteInDbContextScope(provider.Options.DefaultScopeOption, state, task);
		}

		#endregion

		#region Without state

		/// <summary>
		/// <para>
		/// Performs the given <paramref name="task"/>, with access to a new ambient <typeparamref name="TDbContext"/> accessible through <see cref="IDbContextAccessor{TDbContext}"/>.
		/// </para>
		/// <para>
		/// This is the preferred way to perform work in the scope of a <see cref="DbContext"/>. It takes care of many concerns automatically.
		/// </para>
		/// <para>
		/// The task is performed through the <see cref="DbContext"/>'s <see cref="IExecutionStrategy"/>.
		/// The <see cref="IExecutionStrategy"/> may provide behavior such as retry attempts on transient exceptions.
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
		public static TResult ExecuteInDbContextScope<TDbContext, TResult>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			Func<IExecutionScope, TResult> task)
		{
			return provider.ExecuteInDbContextScope(scopeOption, state: task, scope => scope.State(scope));
		}

		/// <summary>
		/// <para>
		/// Performs the given <paramref name="task"/>, with access to a new ambient <typeparamref name="TDbContext"/> accessible through <see cref="IDbContextAccessor{TDbContext}"/>.
		/// </para>
		/// <para>
		/// This is the preferred way to perform work in the scope of a <see cref="DbContext"/>. It takes care of many concerns automatically.
		/// </para>
		/// <para>
		/// The task is performed through the <see cref="DbContext"/>'s <see cref="IExecutionStrategy"/>.
		/// The <see cref="IExecutionStrategy"/> may provide behavior such as retry attempts on transient exceptions.
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
		public static void ExecuteInDbContextScope<TDbContext>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			Action<IExecutionScope> task)
		{
			provider.ExecuteInDbContextScope(scopeOption, state: task, scope => scope.State(scope));
		}

		/// <summary>
		/// <para>
		/// Performs the given <paramref name="task"/>, with access to a new ambient <typeparamref name="TDbContext"/> accessible through <see cref="IDbContextAccessor{TDbContext}"/>.
		/// </para>
		/// <para>
		/// This is the preferred way to perform work in the scope of a <see cref="DbContext"/>. It takes care of many concerns automatically.
		/// </para>
		/// <para>
		/// The task is performed through the <see cref="DbContext"/>'s <see cref="IExecutionStrategy"/>.
		/// The <see cref="IExecutionStrategy"/> may provide behavior such as retry attempts on transient exceptions.
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
		public static TResult ExecuteInDbContextScope<TDbContext, TResult>(this IDbContextProvider<TDbContext> provider,
			Func<IExecutionScope, TResult> task)
		{
			return provider.ExecuteInDbContextScope(provider.Options.DefaultScopeOption, state: task, scope => scope.State(scope));
		}

		/// <summary>
		/// <para>
		/// Performs the given <paramref name="task"/>, with access to a new ambient <typeparamref name="TDbContext"/> accessible through <see cref="IDbContextAccessor{TDbContext}"/>.
		/// </para>
		/// <para>
		/// This is the preferred way to perform work in the scope of a <see cref="DbContext"/>. It takes care of many concerns automatically.
		/// </para>
		/// <para>
		/// The task is performed through the <see cref="DbContext"/>'s <see cref="IExecutionStrategy"/>.
		/// The <see cref="IExecutionStrategy"/> may provide behavior such as retry attempts on transient exceptions.
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
		public static void ExecuteInDbContextScope<TDbContext>(this IDbContextProvider<TDbContext> provider,
			Action<IExecutionScope> task)
		{
			provider.ExecuteInDbContextScope(provider.Options.DefaultScopeOption, state: task, scope => scope.State(scope));
		}

		#endregion
	}
}
