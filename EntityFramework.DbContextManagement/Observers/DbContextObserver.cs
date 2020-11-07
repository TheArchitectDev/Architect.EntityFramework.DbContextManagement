using System;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Architect.EntityFramework.DbContextManagement.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace Architect.EntityFramework.DbContextManagement.Observers
{
	internal sealed class DbContextObserver : IDisposable
	{
		public event Action? WillStartTransaction;
		public event Action? WillCreateCommand;

		/// <summary>
		/// <para>
		/// Callers may assign a single callback to this, which will be awaited when it is invoked.
		/// </para>
		/// <para>
		/// The callback is invoked when changes are about to be saved.
		/// </para>
		/// </summary>
		public Func<bool, CancellationToken, Task>? WillSaveChanges { get; set; }

		/// <summary>
		/// True while a save command is being executed.
		/// </summary>
		private bool IsSaving { get; set; }
		private bool IsTransactionAborted { get; set; }

		private DbContext DbContext { get; }

		private InterceptorSwapper<ISaveChangesInterceptor> SaveInterceptorSwapper { get; }
		private InterceptorSwapper<IDbCommandInterceptor> CommandInterceptorSwapper { get; }
		private InterceptorSwapper<IDbTransactionInterceptor> TransactionInterceptorSwapper { get; }

		public DbContextObserver(DbContext dbContext)
		{
			this.DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

			// Add interceptors
			{
				try
				{
					this.SaveInterceptorSwapper = new InterceptorSwapper<ISaveChangesInterceptor>(this.DbContext,
						new SaveInterceptor(this.InterceptorWillSaveChanges, this.InterceptorDidSaveChanges));
					this.CommandInterceptorSwapper = new InterceptorSwapper<IDbCommandInterceptor>(this.DbContext,
						new CommandInterceptor(this.InterceptorWillCreateCommand, this.InterceptorMightPerformCustomQuery));
					this.TransactionInterceptorSwapper = new InterceptorSwapper<IDbTransactionInterceptor>(this.DbContext,
						new TransactionInterceptor(this.InterceptorWillStartTransaction));
				}
				catch (Exception e)
				{
					// Make sure to dispose what was attached
					this.SaveInterceptorSwapper?.Dispose();
					this.CommandInterceptorSwapper?.Dispose();
					this.TransactionInterceptorSwapper?.Dispose();

					if (e is IncompatibleVersionException)
						throw;
					else
						throw ThrowHelper.ThrowIncompatibleWithEfVersion(e);
				}
			}
		}

		public void Dispose()
		{
			this.WillSaveChanges = null;
			this.WillStartTransaction = null;
			this.WillCreateCommand = null;

			this.SaveInterceptorSwapper.Dispose();
			this.CommandInterceptorSwapper.Dispose();
			this.TransactionInterceptorSwapper.Dispose();
		}

		/// <summary>
		/// As long as it is being observed, treats the wrapped <see cref="Microsoft.EntityFrameworkCore.DbContext"/> as being in a transaction aborted state.
		/// A <see cref="TransactionAbortedException"/> will be thrown whenever any further connectivity is attempted, such as performing a query or starting a transaction.
		/// </summary>
		public void MarkAsTransactionAborted()
		{
			if (this.IsTransactionAborted) return;

			this.IsTransactionAborted = true;

			this.WillStartTransaction += ThrowTransactionAbortedException;
			this.WillCreateCommand += ThrowTransactionAbortedException;
		}

		/// <summary>
		/// Reverts the effect of a single invocation of <see cref="MarkAsTransactionAborted"/>.
		/// </summary>
		public void UnmarkAsTransactionAborted()
		{
			this.IsTransactionAborted = false;

			this.WillStartTransaction -= ThrowTransactionAbortedException;
			this.WillCreateCommand -= ThrowTransactionAbortedException;
		}

		private static void ThrowTransactionAbortedException()
		{
			throw new TransactionAbortedException();
		}

		/// <summary>
		/// Invoked when performing a custom query.
		/// </summary>
		private Task InterceptorMightPerformCustomQuery(bool async, DbCommand command, CancellationToken cancellationToken)
		{
			return !this.IsSaving && this.WillSaveChanges != null
				? PerformAsync()
				: Task.CompletedTask;

			// Local function that performs the work asynchronously
			async Task PerformAsync()
			{
				// Pretend that something is being saved (for example, this allows a transaction to be started)
				var initialTransaction = this.DbContext.Database.CurrentTransaction;

				await this.WillSaveChanges(async, cancellationToken).ConfigureAwait(false);

				var newTransaction = this.DbContext.Database.CurrentTransaction;

				if (newTransaction != initialTransaction)
					command.Transaction = newTransaction.GetDbTransaction();
			}
		}

		private Task InterceptorWillSaveChanges(bool async, CancellationToken cancellationToken)
		{
			Debug.Assert(!this.IsSaving);

			this.IsSaving = true;

			return this.WillSaveChanges is null
				? Task.CompletedTask
				: this.WillSaveChanges(async, cancellationToken);
		}

		private void InterceptorDidSaveChanges(bool success)
		{
			Debug.Assert(this.IsSaving);

			this.IsSaving = false;
		}

		private void InterceptorWillStartTransaction()
		{
			this.WillStartTransaction?.Invoke();
		}

		private void InterceptorWillCreateCommand()
		{
			this.WillCreateCommand?.Invoke();
		}
	}
}
