using System;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Architect.EntityFramework.DbContextManagement.Observers
{
	internal sealed class TransactionInterceptor : DbTransactionInterceptor
	{
		private DbContextScopeOptions Options { get; }

		private Action WillStartTransaction { get; }

		public TransactionInterceptor(DbContextScopeOptions options, Action willStartTransaction)
		{
			System.Diagnostics.Debug.Assert(willStartTransaction is not null);

			this.Options = options;
			this.WillStartTransaction = willStartTransaction;
		}

		public override InterceptionResult<DbTransaction> TransactionStarting(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result)
		{
			this.WillStartTransaction();

			return base.TransactionStarting(connection, eventData, result);
		}

		public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
		{
			this.WillStartTransaction();

			return base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
		}

		public override void TransactionFailed(DbTransaction transaction, TransactionErrorEventData eventData)
		{
			// Use a custom exception type to avoid dangerous "failure on commit" retries, which could lead to duplicate effects if the commit turns out to have succeeded
			if (eventData.Action == "Commit" && this.Options.AvoidFailureOnCommitRetries)
				throw new IOException("The operation failed on commit. Since it is possible that the commit succeeded, potential retries were avoided.", eventData.Exception);

			base.TransactionFailed(transaction, eventData);
		}

		public override Task TransactionFailedAsync(DbTransaction transaction, TransactionErrorEventData eventData, CancellationToken cancellationToken = default)
		{
			// Use a custom exception type to avoid dangerous "failure on commit" retries, which could lead to duplicate effects if the commit turns out to have succeeded
			if (eventData.Action == "Commit" && this.Options.AvoidFailureOnCommitRetries)
				throw new IOException("The operation failed on commit. Since it is possible that the commit succeeded, potential retries were avoided.", eventData.Exception);

			return base.TransactionFailedAsync(transaction, eventData, cancellationToken);
		}
	}
}
