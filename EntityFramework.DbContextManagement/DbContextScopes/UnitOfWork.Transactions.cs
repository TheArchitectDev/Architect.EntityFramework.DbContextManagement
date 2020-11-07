using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.DbContextScopes
{
	internal sealed partial class UnitOfWork<TDbContext>
	{
		internal override bool TryStartTransaction()
		{
			return this.TryRollBackTransactionAsync(async: false, cancellationToken: default)
				.RequireCompleted();
		}

		internal override Task<bool> TryStartTransactionAsync(CancellationToken cancellationToken = default)
		{
			return this.TryStartTransactionAsync(async: true, cancellationToken);
		}

		internal override async Task<bool> TryStartTransactionAsync(bool async, CancellationToken cancellationToken = default)
		{
			if (this.DbContext.Database.CurrentTransaction != null) return false;

			using var exclusiveLock = this.GetLock();

			if (this.DbContext.Database.CurrentTransaction != null) return false;

			// #TODO: Consider if we should hide Transaction.Current if there is one!!
			if (Transaction.Current != null)
				throw new InvalidOperationException("An ambient transaction has been detected. Scoped execution does not support ambient transactions.");

			var isolationLevel = this.IsolationLevel;

			if (async)
			{
				await (isolationLevel is null
					? this.DbContext.Database.BeginTransactionAsync(cancellationToken)
					: this.DbContext.Database.BeginTransactionAsync(isolationLevel.Value, cancellationToken));
			}
			else
			{
				_ = isolationLevel is null
					? this.DbContext.Database.BeginTransaction()
					: this.DbContext.Database.BeginTransaction(isolationLevel.Value);
			}

			return true;
		}

		internal override bool TryCommitTransaction()
		{
			if (this.DbContext.Database.CurrentTransaction is null) return false;

			using var exclusiveLock = this.GetLock();

			if (this.DbContext.Database.CurrentTransaction is null) return false;

			this.DbContext.Database.CurrentTransaction.Commit();

			System.Diagnostics.Debug.Assert(this.DbContext.Database.CurrentTransaction is null, "The DatabaseFacade should have unset its own transaction.");

			return true;
		}

		internal override async Task<bool> TryCommitTransactionAsync(CancellationToken cancellationToken = default)
		{
			if (this.DbContext.Database.CurrentTransaction is null) return false;

			using var exclusiveLock = this.GetLock();

			if (this.DbContext.Database.CurrentTransaction is null) return false;

			await this.DbContext.Database.CurrentTransaction.CommitAsync(cancellationToken);

			System.Diagnostics.Debug.Assert(this.DbContext.Database.CurrentTransaction is null, "The DatabaseFacade should have unset its own transaction.");

			return true;
		}

		internal override Task<bool> TryCommitTransactionAsync(bool async, CancellationToken cancellationToken = default)
		{
			if (async) return this.TryCommitTransactionAsync(cancellationToken);

			var result = this.TryCommitTransaction();
			return Task.FromResult(result);
		}

		internal override bool TryRollBackTransaction()
		{
			return this.TryRollBackTransactionAsync(async: false, cancellationToken: default)
				.RequireCompleted();
		}

		internal override Task<bool> TryRollBackTransactionAsync(CancellationToken cancellationToken)
		{
			return this.TryRollBackTransactionAsync(async: true, cancellationToken);
		}

		internal override async Task<bool> TryRollBackTransactionAsync(bool async, CancellationToken cancellationToken)
		{
			if (this.DbContext.Database.CurrentTransaction is null) return false;

			using var exclusiveLock = this.GetLock();

			if (this.DbContext.Database.CurrentTransaction is null) return false;

			if (async) await this.DbContext.Database.CurrentTransaction.RollbackAsync(cancellationToken);
			else this.DbContext.Database.CurrentTransaction.Rollback();

			System.Diagnostics.Debug.Assert(this.DbContext.Database.CurrentTransaction is null, "The DatabaseFacade should have unset its own transaction.");

			return true;
		}

		internal override bool TryRollBackTransactionAndInvalidate()
		{
			return this.TryRollBackTransactionAndInvalidateAsync(async: false, cancellationToken: default)
				.RequireCompleted();
		}

		internal override Task<bool> TryRollBackTransactionAndInvalidateAsync(CancellationToken cancellationToken)
		{
			return this.TryRollBackTransactionAndInvalidateAsync(async: true, cancellationToken);
		}

		internal override async Task<bool> TryRollBackTransactionAndInvalidateAsync(bool async, CancellationToken cancellationToken)
		{
			// Throw whenever any further connectivity is attempted
			this.DbContextObserver.MarkAsTransactionAborted();

			var didRollBack = await this.TryRollBackTransactionAsync(async, cancellationToken);
			return didRollBack;
		}

		internal override void UndoInvalidation()
		{
			// Do not throw whenever any further connectivity is attempted
			this.DbContextObserver.UnmarkAsTransactionAborted();
		}
	}
}
