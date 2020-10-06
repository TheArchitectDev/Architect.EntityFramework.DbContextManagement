using System;
using System.Data;
using System.Threading.Tasks;
using Architect.EntityFramework.DbContextManagement.DbContextScopes;
using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.ExecutionScopes
{
	internal sealed class ExecutionScope<TState> : IExecutionScope<TState>, IAsyncDisposable, IDisposable
	{
		internal bool IsCompleted { get; private set; } = true;

		public IsolationLevel? IsolationLevel
		{
			get => this.UnitOfWork.IsolationLevel;
			set => this.UnitOfWork.IsolationLevel = value;
		}

		public UnitOfWork UnitOfWork { get; }
		public DbContext DbContext { get; }
		public bool IsNested { get; }
		public TState State { get; }

		public ExecutionScope(UnitOfWork unitOfWork, DbContext dbContext, bool isNested, TState state)
		{
			this.UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
			this.DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
			this.IsNested = isNested;
			this.State = state;
		}

		public void Abort()
		{
			this.IsCompleted = false;
		}

		public void Dispose()
		{
			if (!this.IsCompleted)
				this.UnitOfWork.TryRollBackTransactionAndInvalidate();
		}

		public ValueTask DisposeAsync()
		{
			if (!this.IsCompleted)
				return new ValueTask(this.UnitOfWork.TryRollBackTransactionAndInvalidateAsync(cancellationToken: default));

			return new ValueTask(Task.CompletedTask);
		}
	}
}
