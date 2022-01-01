using Architect.AmbientContexts;

namespace Architect.EntityFramework.DbContextManagement.Tests.Providers.DbContextProviderTests.ScopedExecutionTests
{
	public abstract class ScopedExecutionTestBase : DbContextProviderTestBase
	{
		public enum Overload : byte
		{
			// With nothing
			Async,
			AsyncResult,
			Sync,
			SyncResult,

			// With scope option
			AsyncWithScopeOption,
			AsyncResultWithScopeOption,
			SyncWithScopeOption,
			SyncResultWithScopeOption,

			// With cancellation
			AsyncWithCancellation,
			AsyncResultWithCancellation,

			// With scope option and cancellation
			AsyncWithScopeOptionWithCancellation,
			AsyncResultWithScopeOptionWithCancellation,

			// With state (with state always has cancellation for async)
			AsyncWithState,
			AsyncResultWithState,
			SyncWithState,
			SyncResultWithState,

			// With scope option and state (with state always has cancellation for async)
			AsyncWithScopeOptionWithState,
			AsyncResultWithScopeOptionWithState,
			SyncWithScopeOptionWithState,
			SyncResultWithScopeOptionWithState,
		}

		public static IEnumerable<object[]> GetOverloads() => Overloads.Select(o => new[] { (object)o });
		public static IEnumerable<object[]> GetOverloadsWithResult() => OverloadsWithResult.Select(o => new[] { (object)o });
		public static IEnumerable<object[]> GetOverloadsWithoutResult() => OverloadsWithoutResult.Select(o => new[] { (object)o });
		public static IEnumerable<object[]> GetOverloadsWithScopeOption() => OverloadsWithScopeOption.Select(o => new[] { (object)o });
		private static readonly Overload[] Overloads = (Overload[])Enum.GetValues(typeof(Overload));
		private static readonly Overload[] OverloadsWithResult = Overloads.Where(o => o.ToString().Contains("Result")).ToArray();
		private static readonly Overload[] OverloadsWithoutResult = Overloads.Except(OverloadsWithResult).ToArray();
		private static readonly Overload[] OverloadsWithScopeOption = Overloads.Where(o => o.ToString().Contains("WithScopeOption")).ToArray();

		protected bool ExpectsResult(Overload overload)
		{
			return overload.ToString().Contains("Result");
		}

		protected bool ExpectsState(Overload overload)
		{
			return overload.ToString().Contains("WithState");
		}

		protected bool IsAsync(Overload overload)
		{
			return overload.ToString().Contains("Async");
		}

		protected bool ExtractState(IExecutionScope scope)
		{
			if (scope is not IExecutionScope<bool> scopeWithBoolState)
				throw new Exception($"{scope} does not have bool state.");
			return scopeWithBoolState.State;
		}

		/// <summary>
		/// Executes the requested overload of <see cref="IDbContextProvider{TDbContext}.ExecuteInDbContextScope{TState, TResult}"/> or its async variant.
		/// </summary>
		private protected async Task<TResult> Execute<TResult>(Overload overload, IDbContextProvider<TestDbContext> provider, Func<IExecutionScope, CancellationToken, Task<TResult>> task,
			AmbientScopeOption scopeOption = AmbientScopeOption.JoinExisting,
			CancellationToken cancellationToken = default)
		{
			switch (overload)
			{
				case Overload.Async:
					await provider.ExecuteInDbContextScopeAsync(WithoutResultWithoutCancellation(task));
					return default;
				case Overload.AsyncResult:
					return await provider.ExecuteInDbContextScopeAsync(WithoutCancellation(task));
				case Overload.Sync:
					provider.ExecuteInDbContextScope(SyncWithoutResult(task));
					return default;
				case Overload.SyncResult:
					return provider.ExecuteInDbContextScope(Sync(task));
				case Overload.AsyncWithScopeOption:
					await provider.ExecuteInDbContextScopeAsync(scopeOption, WithoutResultWithoutCancellation(task));
					return default;
				case Overload.AsyncResultWithScopeOption:
					return await provider.ExecuteInDbContextScopeAsync(scopeOption, WithoutCancellation(task));
				case Overload.SyncWithScopeOption:
					provider.ExecuteInDbContextScope(scopeOption, SyncWithoutResult(task));
					return default;
				case Overload.SyncResultWithScopeOption:
					return provider.ExecuteInDbContextScope(scopeOption, Sync(task));
				case Overload.AsyncWithCancellation:
					await provider.ExecuteInDbContextScopeAsync(cancellationToken, WithoutResult(task));
					return default;
				case Overload.AsyncResultWithCancellation:
					return await provider.ExecuteInDbContextScopeAsync(cancellationToken, task);
				case Overload.AsyncWithScopeOptionWithCancellation:
					await provider.ExecuteInDbContextScopeAsync(scopeOption, cancellationToken, WithoutResult(task));
					return default;
				case Overload.AsyncResultWithScopeOptionWithCancellation:
					return await provider.ExecuteInDbContextScopeAsync(scopeOption, cancellationToken, task);
				case Overload.AsyncWithState:
					await provider.ExecuteInDbContextScopeAsync(true, cancellationToken, WithoutResult(task));
					return default;
				case Overload.AsyncResultWithState:
					return await provider.ExecuteInDbContextScopeAsync(true, cancellationToken, task);
				case Overload.SyncWithState:
					provider.ExecuteInDbContextScope(true, SyncWithoutResult(task));
					return default;
				case Overload.SyncResultWithState:
					return provider.ExecuteInDbContextScope(true, Sync(task));
				case Overload.AsyncWithScopeOptionWithState:
					await provider.ExecuteInDbContextScopeAsync(scopeOption, true, cancellationToken, WithoutResult(task));
					return default;
				case Overload.AsyncResultWithScopeOptionWithState:
					return await provider.ExecuteInDbContextScopeAsync(scopeOption, true, cancellationToken, task);
				case Overload.SyncWithScopeOptionWithState:
					provider.ExecuteInDbContextScope(scopeOption, true, SyncWithoutResult(task));
					return default;
				case Overload.SyncResultWithScopeOptionWithState:
					return provider.ExecuteInDbContextScope(scopeOption, true, Sync(task));
				default:
					throw new NotImplementedException();
			}
		}

		private static Func<IExecutionScope, TResult> Sync<TResult>(Func<IExecutionScope, CancellationToken, Task<TResult>> task)
		{
			return scope => task(scope, default).RequireCompleted();
		}

		private static Func<IExecutionScope, CancellationToken, Task> WithoutResult<TResult>(Func<IExecutionScope, CancellationToken, Task<TResult>> task)
		{
			return async (scope, ct) => { await task(scope, ct); };
		}

		private static Action<IExecutionScope> SyncWithoutResult<TResult>(Func<IExecutionScope, CancellationToken, Task<TResult>> task)
		{
			return scope => { Sync(task)(scope); };
		}

		private static Func<IExecutionScope, Task<TResult>> WithoutCancellation<TResult>(Func<IExecutionScope, CancellationToken, Task<TResult>> task)
		{
			return scope => task(scope, default);
		}

		private static Func<IExecutionScope, Task> WithoutResultWithoutCancellation<TResult>(Func<IExecutionScope, CancellationToken, Task<TResult>> task)
		{
			return scope => WithoutResult(task)(scope, default);
		}
	}
}
