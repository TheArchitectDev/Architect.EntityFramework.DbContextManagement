using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Architect.EntityFramework.DbContextManagement.Observers
{
	internal sealed class TransactionInterceptor : DbTransactionInterceptor
	{
		private Action WillStartTransaction { get; }

		public TransactionInterceptor(Action willStartTransaction)
		{
			System.Diagnostics.Debug.Assert(willStartTransaction is not null);

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
	}
}
