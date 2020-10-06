using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Architect.EntityFramework.DbContextManagement.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Architect.EntityFramework.DbContextManagement.Observers
{
	internal sealed class DbContextObserver : IObserver<KeyValuePair<string, object?>>, IDisposable
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

		/// <summary>
		/// A subscription token to the <see cref="DbContext"/>'s <see cref="DiagnosticListener"/>.
		/// </summary>
		private IDisposable? SubscriptionToken { get; set; }
		private InterceptorSwapper<ISaveChangesInterceptor> SaveInterceptorSwapper { get; }
		private InterceptorSwapper<IDbCommandInterceptor> CommandInterceptorSwapper { get; }

		public DbContextObserver(DbContext dbContext)
		{
			this.DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

			// Add a DiagnosticListener
			{
				var diagnosticSource = dbContext.GetService<DiagnosticSource>() ?? ThrowHelper.ThrowIncompatibleWithEfVersion<DiagnosticSource>(
					new Exception($"The DbContext ({dbContext.GetType().Name}) no longer provides a {nameof(DiagnosticSource)}."));
				var diagnosticListener = diagnosticSource as DiagnosticListener ?? ThrowHelper.ThrowIncompatibleWithEfVersion<DiagnosticListener>(
					new Exception($"The DbContext ({dbContext.GetType().Name}) now provides a {nameof(DiagnosticSource)} that is not a {nameof(DiagnosticListener)}."));

				this.SubscriptionToken = diagnosticListener.Subscribe(this, this.IsDiagnosticListenerEnabledForEvent);
			}

			// Add interceptors
			{
				try
				{
					this.SaveInterceptorSwapper = new InterceptorSwapper<ISaveChangesInterceptor>(this.DbContext,
						new SaveInterceptor(this.InterceptorWillSaveChanges, this.InterceptorDidSaveChanges));
					this.CommandInterceptorSwapper = new InterceptorSwapper<IDbCommandInterceptor>(this.DbContext,
						new CommandInterceptor(this.InterceptorWillCreateCommand, this.InterceptorMightPerformCustomQuery));
				}
				catch (Exception e)
				{
					// Make sure to dispose what was attached
					this.SaveInterceptorSwapper?.Dispose();
					this.CommandInterceptorSwapper?.Dispose();

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

			this.SubscriptionToken?.Dispose();
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

				await this.WillSaveChanges(async, cancellationToken);

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

		/// <summary>
		/// We always return false, but we use this method to intercept certain events.
		/// </summary>
		private bool IsDiagnosticListenerEnabledForEvent(string eventName, object? a, object? b)
		{
			switch (eventName)
			{
				// #TODO: Use DbTransactionInterceptor
				// Could use a DbTransactionInterceptor instead, in the future
				case "Microsoft.EntityFrameworkCore.Database.Transaction.TransactionStarting":
					this.InterceptorWillStartTransaction();
					break;
			}

			return false;
		}

		/// <summary>
		/// Required by IObserver.
		/// </summary>
		public void OnNext(KeyValuePair<string, object?> value)
		{
		}

		/// <summary>
		/// Required by IObserver.
		/// </summary>
		public void OnCompleted()
		{
		}

		/// <summary>
		/// Required by IObserver.
		/// </summary>
		public void OnError(Exception error)
		{
		}
	}
}
