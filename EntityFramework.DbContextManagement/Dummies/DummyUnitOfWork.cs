using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Architect.EntityFramework.DbContextManagement.DbContextScopes;

namespace Architect.EntityFramework.DbContextManagement.Dummies
{
	internal sealed class DummyUnitOfWork : UnitOfWork
	{
		private bool ThrowsTransactionAborted { get; set; }

		private bool IsInTransaction { get; set; }

		private static void ThrowTransactionAborted() => throw new TransactionAbortedException();

		internal override void TryAddAutoFlushBehavior()
		{
		}

		internal override void TryRemoveAutoFlushBehavior()
		{
		}

		internal override bool TryStartTransaction()
		{
			if (this.ThrowsTransactionAborted) ThrowTransactionAborted();

			var result = !this.IsInTransaction;
			this.IsInTransaction = true;
			return result;
		}

		internal override Task<bool> TryStartTransactionAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(this.TryStartTransaction());
		}

		internal override Task<bool> TryStartTransactionAsync(bool async, CancellationToken cancellationToken)
		{
			return Task.FromResult(this.TryStartTransaction());
		}

		internal override bool TryCommitTransaction()
		{
			if (this.ThrowsTransactionAborted) ThrowTransactionAborted();

			var result = this.IsInTransaction;
			this.IsInTransaction = false;
			return result;
		}

		internal override Task<bool> TryCommitTransactionAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(this.TryCommitTransaction());
		}

		internal override Task<bool> TryCommitTransactionAsync(bool async, CancellationToken cancellationToken)
		{
			return Task.FromResult(this.TryCommitTransaction());
		}

		internal override bool TryRollBackTransaction()
		{
			if (this.ThrowsTransactionAborted) ThrowTransactionAborted();

			var result = this.IsInTransaction;
			this.IsInTransaction = false;
			return result;
		}

		internal override Task<bool> TryRollBackTransactionAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(this.TryRollBackTransaction());
		}

		internal override Task<bool> TryRollBackTransactionAsync(bool async, CancellationToken cancellationToken)
		{
			return Task.FromResult(this.TryRollBackTransaction());
		}

		internal override bool TryRollBackTransactionAndInvalidate()
		{
			var wasInTransaction = this.TryRollBackTransaction();

			this.ThrowsTransactionAborted = true;

			return wasInTransaction;
		}

		internal override Task<bool> TryRollBackTransactionAndInvalidateAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(this.TryRollBackTransactionAndInvalidate());
		}

		internal override Task<bool> TryRollBackTransactionAndInvalidateAsync(bool async, CancellationToken cancellationToken)
		{
			return Task.FromResult(this.TryRollBackTransactionAndInvalidate());
		}

		internal override void UndoInvalidation()
		{
			this.ThrowsTransactionAborted = false;
		}
	}
}
