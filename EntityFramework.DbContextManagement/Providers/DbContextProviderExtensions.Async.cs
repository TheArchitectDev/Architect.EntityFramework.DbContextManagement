using System;
using System.Threading;
using System.Threading.Tasks;
using Architect.AmbientContexts;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	// #TODO: Summaries
	public static partial class DbContextProviderExtensions
	{
		#region With state

		// Already implemented by default interface implementation
		//public static Task<TResult> ExecuteInDbContextScopeAsync<TDbContext, TState, TResult>(this IDbContextProvider<TDbContext> provider,
		//	AmbientScopeOption scopeOption,
		//	TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		//{
		//	return provider.ExecuteInDbContextScopeAsync(scopeOption, state, cancellationToken, task);
		//}

		internal static Task ExecuteInDbContextScopeAsync<TDbContext, TState>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task> task)
		{
			return provider.ExecuteInDbContextScopeAsync(scopeOption, state, cancellationToken, ExecuteAndReturnTrue);

			// Local function that executes the given task and returns true
			async Task<bool> ExecuteAndReturnTrue(IExecutionScope<TState> executionScope, CancellationToken cancellationToken)
			{
				await task(executionScope, cancellationToken);
				return true;
			}
		}
		/*
		public static Task<TResult> ExecuteInDbContextScopeAsync<TDbContext, TState, TResult>(this IDbContextProvider<TDbContext> provider,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			return provider.ExecuteInDbContextScopeAsync(provider.Options.DefaultScopeOption, state, cancellationToken, task);
		}

		public static Task ExecuteInDbContextScopeAsync<TDbContext, TState>(this IDbContextProvider<TDbContext> provider,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task> task)
		{
			return provider.ExecuteInDbContextScopeAsync(provider.Options.DefaultScopeOption, state, cancellationToken, task);
		}
		*/

		#endregion

		#region Without state

		public static Task<TResult> ExecuteInDbContextScopeAsync<TDbContext, TResult>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			CancellationToken cancellationToken, Func<IExecutionScope, CancellationToken, Task<TResult>> task)
		{
			return provider.ExecuteInDbContextScopeAsync(scopeOption, state: task, cancellationToken, (scope, ct) => scope.State(scope, ct));
		}

		public static Task ExecuteInDbContextScopeAsync<TDbContext>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			CancellationToken cancellationToken, Func<IExecutionScope, CancellationToken, Task> task)
		{
			return provider.ExecuteInDbContextScopeAsync(scopeOption, state: task, cancellationToken, (scope, ct) => scope.State(scope, ct));
		}

		public static Task<TResult> ExecuteInDbContextScopeAsync<TDbContext, TResult>(this IDbContextProvider<TDbContext> provider,
			CancellationToken cancellationToken, Func<IExecutionScope, CancellationToken, Task<TResult>> task)
		{
			return provider.ExecuteInDbContextScopeAsync(provider.Options.DefaultScopeOption, state: task, cancellationToken, (scope, ct) => scope.State(scope, ct));
		}

		public static Task ExecuteInDbContextScopeAsync<TDbContext>(this IDbContextProvider<TDbContext> provider,
			CancellationToken cancellationToken, Func<IExecutionScope, CancellationToken, Task> task)
		{
			return provider.ExecuteInDbContextScopeAsync(provider.Options.DefaultScopeOption, state: task, cancellationToken, (scope, ct) => scope.State(scope, ct));
		}

		#endregion

		#region Without state and without cancellation

		public static Task<TResult> ExecuteInDbContextScopeAsync<TDbContext, TResult>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			Func<IExecutionScope, Task<TResult>> task)
		{
			return provider.ExecuteInDbContextScopeAsync(scopeOption, state: task, default, (scope, _) => scope.State(scope));
		}

		public static Task ExecuteInDbContextScopeAsync<TDbContext>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			Func<IExecutionScope, Task> task)
		{
			return provider.ExecuteInDbContextScopeAsync(scopeOption, state: task, default, (scope, _) => scope.State(scope));
		}

		public static Task<TResult> ExecuteInDbContextScopeAsync<TDbContext, TResult>(this IDbContextProvider<TDbContext> provider,
			Func<IExecutionScope, Task<TResult>> task)
		{
			return provider.ExecuteInDbContextScopeAsync(provider.Options.DefaultScopeOption, state: task, default, (scope, _) => scope.State(scope));
		}

		public static Task ExecuteInDbContextScopeAsync<TDbContext>(this IDbContextProvider<TDbContext> provider,
			Func<IExecutionScope, Task> task)
		{
			return provider.ExecuteInDbContextScopeAsync(provider.Options.DefaultScopeOption, state: task, default, (scope, _) => scope.State(scope));
		}

		#endregion
	}
}
