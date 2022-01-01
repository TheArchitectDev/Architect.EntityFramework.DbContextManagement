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

		private DbContext DbContext { get; }

		public DummyExecutionStrategy(DbContext dbContext)
		{
			this.DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		}

		public TResult Execute<TState, TResult>(TState state, Func<DbContext, TState, TResult> operation, Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
		{
			return operation(this.DbContext, state);
		}

		public Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
			Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
			CancellationToken cancellationToken = default)
		{
			return operation(this.DbContext, state, cancellationToken);
		}
	}
}
