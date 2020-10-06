using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Architect.EntityFramework.DbContextManagement.Dummies
{
	internal sealed class DummyExecutionStrategy : IExecutionStrategy
	{
		public bool RetriesOnFailure => false;

		private bool ThrowsConcurrencyExceptionOnNextExecution { get; set; }

		private DbContext DbContext { get; }

		public DummyExecutionStrategy(DbContext dbContext, bool throwsConcurrencyExceptionOnNextExecution = false)
		{
			this.DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
			this.ThrowsConcurrencyExceptionOnNextExecution = throwsConcurrencyExceptionOnNextExecution;
		}

		public TResult Execute<TState, TResult>(TState state, Func<DbContext, TState, TResult> operation, Func<DbContext, TState, ExecutionResult<TResult>> verifySucceeded)
		{
			this.ThrowConcurrencyExceptionIfRequested();

			return operation(this.DbContext, state);
		}

		public Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken = default)
		{
			this.ThrowConcurrencyExceptionIfRequested();

			return operation(this.DbContext, state, cancellationToken);
		}

		// #TODO: This is a bit early - we want this just before the commit. Probably remove this.
		private void ThrowConcurrencyExceptionIfRequested()
		{
			if (!this.ThrowsConcurrencyExceptionOnNextExecution) return;

			this.ThrowsConcurrencyExceptionOnNextExecution = false;
			throw new DbUpdateConcurrencyException("This is a simulated optimistic concurrency exception.");
		}
	}
}
