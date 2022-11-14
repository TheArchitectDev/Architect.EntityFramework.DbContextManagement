using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Architect.EntityFramework.DbContextManagement.ExecutionStrategies
{
	/// <summary>
	/// <para>
	/// Wraps a given <see cref="IExecutionStrategy"/> and adds the behavior of retrying on a optimistic concurrency conflicts, as detected by the <see cref="DbUpdateConcurrencyException"/>.
	/// </para>
	/// <para>
	/// Note that the retry attempt, just like that of other <see cref="IExecutionStrategy"/> types, does not create a fresh <see cref="DbContext"/>.
	/// As part of the (retryable) work passed to the strategy, it is strongly advisable to reset the <see cref="DbContext"/> if any exception occurs.
	/// </para>
	/// <para>
	/// Unfortunately, a concurrency conflict resets the retry count of the nested <see cref="IExecutionStrategy"/>.
	/// While this could lead to more attempts than usual in cases where there are both connection and concurrency issues, the number of attempts does remain bounded.
	/// </para>
	/// </summary>
	internal sealed class RetryOnOptimisticConcurrencyFailureExecutionStrategy : IExecutionStrategy
	{
		/// <summary>
		/// Up to this many retries after the initial attempt.
		/// </summary>
		private const int MaxRetryCount = 2;

		private int RetryCount { get; set; }
		private bool RetriesOnOptimisticConcurrencyFailure => this.RetryCount < MaxRetryCount;

		public bool RetriesOnFailure => this.WrappedStrategy.RetriesOnFailure || this.RetriesOnOptimisticConcurrencyFailure; // Note that by definition this indicates whether we MIGHT retry on failure

		private IExecutionStrategy WrappedStrategy { get; }

		public RetryOnOptimisticConcurrencyFailureExecutionStrategy(IExecutionStrategy wrappedStrategy)
		{
			this.WrappedStrategy = wrappedStrategy ?? throw new ArgumentNullException(nameof(wrappedStrategy));
		}

		public TResult Execute<TState, TResult>(TState state, Func<DbContext, TState, TResult> operation, Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
		{
			var result = this.ExecuteCore(async: false,
				state: (Self: this, State: state, Operation: operation, VerifySucceeded: verifySucceeded),
				state => PerformSynchronously(state.Self, state.State, state.Operation, state.VerifySucceeded));

			return result.RequireCompleted();

			// Local function that synchronously performs the operation and returns the result wrapped in a task
			static Task<TResult> PerformSynchronously(RetryOnOptimisticConcurrencyFailureExecutionStrategy self,
				TState state, Func<DbContext, TState, TResult> operation, Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
			{
				var result = self.WrappedStrategy.Execute(state, operation, verifySucceeded);
				return Task.FromResult(result);
			}
		}

		public Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
			Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded, CancellationToken cancellationToken = default)
		{
			var result = this.ExecuteCore(async: true,
				state: (Self: this, State: state, Operation: operation, VerifySucceeded: verifySucceeded, CancellationToken: cancellationToken),
				state => state.Self.WrappedStrategy.ExecuteAsync(state.State, state.Operation, state.VerifySucceeded, state.CancellationToken));

			return result;
		}

		private async Task<TResult> ExecuteCore<TState, TResult>(bool async, TState state, Func<TState, Task<TResult>> operation)
		{
			while (true)
			{
				try
				{
					var result = async
						? await operation(state)
						: operation(state).RequireCompleted();

					return result;
				}
				catch (DbUpdateConcurrencyException) when (this.RetriesOnOptimisticConcurrencyFailure)
				{
					this.RetryCount++;
				}
				catch (DbUpdateConcurrencyException e)
				{
					throw new RetryLimitExceededException("The retry limit was reached while retrying because of an optimistic concurrency conflict.", e);
				}
			}
		}
	}
}
