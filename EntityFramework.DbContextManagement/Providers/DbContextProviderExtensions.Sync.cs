using System;
using Architect.AmbientContexts;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	// #TODO: Summaries
	public static partial class DbContextProviderExtensions
	{
		#region With state

		// Already implemented by default interface implementation
		//public static TResult ExecuteInDbContextScope<TDbContext, TState, TResult>(this IDbContextProvider<TDbContext> provider,
		//	AmbientScopeOption scopeOption,
		//	TState state, Func<IExecutionScope<TState>, TResult> task)
		//{
		//	return provider.ExecuteInDbContextScope(scopeOption, state, task);
		//}

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
		/*
		public static TResult ExecuteInDbContextScope<TDbContext, TState, TResult>(this IDbContextProvider<TDbContext> provider,
			TState state, Func<IExecutionScope<TState>, TResult> task)
		{
			return provider.ExecuteInDbContextScope(provider.Options.DefaultScopeOption, state, task);
		}

		public static void ExecuteInDbContextScope<TDbContext, TState>(this IDbContextProvider<TDbContext> provider,
			TState state, Action<IExecutionScope<TState>> task)
		{
			provider.ExecuteInDbContextScope(provider.Options.DefaultScopeOption, state, task);
		}
		*/
		#endregion

		#region Without state

		public static TResult ExecuteInDbContextScope<TDbContext, TResult>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			Func<IExecutionScope, TResult> task)
		{
			return provider.ExecuteInDbContextScope(scopeOption, state: task, scope => scope.State(scope));
		}

		public static void ExecuteInDbContextScope<TDbContext>(this IDbContextProvider<TDbContext> provider,
			AmbientScopeOption scopeOption,
			Action<IExecutionScope> task)
		{
			provider.ExecuteInDbContextScope(scopeOption, state: task, scope => scope.State(scope));
		}

		public static TResult ExecuteInDbContextScope<TDbContext, TResult>(this IDbContextProvider<TDbContext> provider,
			Func<IExecutionScope, TResult> task)
		{
			return provider.ExecuteInDbContextScope(provider.Options.DefaultScopeOption, state: task, scope => scope.State(scope));
		}

		public static void ExecuteInDbContextScope<TDbContext>(this IDbContextProvider<TDbContext> provider,
			Action<IExecutionScope> task)
		{
			provider.ExecuteInDbContextScope(provider.Options.DefaultScopeOption, state: task, scope => scope.State(scope));
		}

		#endregion
	}
}
