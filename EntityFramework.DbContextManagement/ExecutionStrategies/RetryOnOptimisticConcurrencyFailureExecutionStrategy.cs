using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Architect.EntityFramework.DbContextManagement.ExecutionStrategies
{
	/// <summary>
	/// <para>
	/// Wraps a given <see cref="IExecutionStrategy"/> and adds the behavior of retrying on a optimistic concurrency conflicts, as detected by the <see cref="DBConcurrencyException"/>.
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

		public TResult Execute<TState, TResult>(TState state, Func<DbContext, TState, TResult> operation, Func<DbContext, TState, ExecutionResult<TResult>> verifySucceeded)
		{
			while (true)
			{
				try
				{
					var result = this.WrappedStrategy.Execute(state, operation, verifySucceeded);
					return result;
				}
				catch (DbUpdateConcurrencyException) when (this.RetriesOnOptimisticConcurrencyFailure)
				{
					this.RetryCount++;
				}
			}
		}

		public async Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
			Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken = default)
		{
			while (true)
			{
				try
				{
					var result = await this.WrappedStrategy.ExecuteAsync(state, operation, verifySucceeded, cancellationToken);
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
