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
	public static partial class DbContextProviderExtensions
	{
		#region With state

		// This primary overload is specified on the interface itself
		//public static Task<TResult> ExecuteInDbContextScopeAsync<TDbContext, TState, TResult>(this IDbContextProvider<TDbContext> provider,
		//	AmbientScopeOption scopeOption,
		//	TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		//{
		//	return provider.ExecuteInDbContextScopeAsync(scopeOption, state, cancellationToken, task);
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
		internal static Task ExecuteInDbContextScopeAsync<TDbContext, TState>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task> task)
		{
			return provider.ExecuteInDbContextScopeAsync(scopeOption, state, cancellationToken, ExecuteAndReturnTrue);

			// Local function that executes the given task and returns true
			async Task<bool> ExecuteAndReturnTrue(IExecutionScope<TState> executionScope, CancellationToken cancellationToken)
			{
				await task(executionScope, cancellationToken).ConfigureAwait(false);
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
		public static Task<TResult> ExecuteInDbContextScopeAsync<TDbContext, TState, TResult>(this IDbContextProvider<TDbContext> provider,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			return provider.ExecuteInDbContextScopeAsync(provider.Options.DefaultScopeOption, state, cancellationToken, task);
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
		public static Task ExecuteInDbContextScopeAsync<TDbContext, TState>(this IDbContextProvider<TDbContext> provider,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task> task)
		{
			return provider.ExecuteInDbContextScopeAsync(provider.Options.DefaultScopeOption, state, cancellationToken, task);
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
		public static Task<TResult> ExecuteInDbContextScopeAsync<TDbContext, TResult>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			CancellationToken cancellationToken, Func<IExecutionScope, CancellationToken, Task<TResult>> task)
		{
			return provider.ExecuteInDbContextScopeAsync(scopeOption, state: task, cancellationToken, (scope, ct) => scope.State(scope, ct));
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
		public static Task ExecuteInDbContextScopeAsync<TDbContext>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			CancellationToken cancellationToken, Func<IExecutionScope, CancellationToken, Task> task)
		{
			return provider.ExecuteInDbContextScopeAsync(scopeOption, state: task, cancellationToken, (scope, ct) => scope.State(scope, ct));
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
		public static Task<TResult> ExecuteInDbContextScopeAsync<TDbContext, TResult>(this IDbContextProvider<TDbContext> provider,
			CancellationToken cancellationToken, Func<IExecutionScope, CancellationToken, Task<TResult>> task)
		{
			return provider.ExecuteInDbContextScopeAsync(provider.Options.DefaultScopeOption, state: task, cancellationToken, (scope, ct) => scope.State(scope, ct));
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
		public static Task ExecuteInDbContextScopeAsync<TDbContext>(this IDbContextProvider<TDbContext> provider,
			CancellationToken cancellationToken, Func<IExecutionScope, CancellationToken, Task> task)
		{
			return provider.ExecuteInDbContextScopeAsync(provider.Options.DefaultScopeOption, state: task, cancellationToken, (scope, ct) => scope.State(scope, ct));
		}

		#endregion

		#region Without state and without cancellation

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
		public static Task<TResult> ExecuteInDbContextScopeAsync<TDbContext, TResult>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			Func<IExecutionScope, Task<TResult>> task)
		{
			return provider.ExecuteInDbContextScopeAsync(scopeOption, state: task, default, (scope, _) => scope.State(scope));
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
		public static Task ExecuteInDbContextScopeAsync<TDbContext>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			Func<IExecutionScope, Task> task)
		{
			return provider.ExecuteInDbContextScopeAsync(scopeOption, state: task, default, (scope, _) => scope.State(scope));
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
		public static Task<TResult> ExecuteInDbContextScopeAsync<TDbContext, TResult>(this IDbContextProvider<TDbContext> provider,
			Func<IExecutionScope, Task<TResult>> task)
		{
			return provider.ExecuteInDbContextScopeAsync(provider.Options.DefaultScopeOption, state: task, default, (scope, _) => scope.State(scope));
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
		public static Task ExecuteInDbContextScopeAsync<TDbContext>(this IDbContextProvider<TDbContext> provider,
			Func<IExecutionScope, Task> task)
		{
			return provider.ExecuteInDbContextScopeAsync(provider.Options.DefaultScopeOption, state: task, default, (scope, _) => scope.State(scope));
		}

		#endregion
	}
}
