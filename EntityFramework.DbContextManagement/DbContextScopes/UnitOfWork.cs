using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Architect.EntityFramework.DbContextManagement.Locking;
using Architect.EntityFramework.DbContextManagement.Observers;
using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.DbContextScopes
{
	/// <summary>
	/// <para>
	/// The unit of work shared by a scope and its effective parents and children.
	/// </para>
	/// <para>
	/// The unit of work's lifetime matches that of the effective outermost scope.
	/// </para>
	/// </summary>
	internal abstract class UnitOfWork
	{
		/// <summary>
		/// Lazily instantiated.
		/// </summary>
		public abstract DbContextObserver DbContextObserver { get; }

		internal System.Data.IsolationLevel? IsolationLevel
		{
			get => this._isolationLevel;
			set
			{
				// If trying to change an already-set value
				if (this._isolationLevel != null && value != this._isolationLevel)
					throw new InvalidOperationException($"Attempted to set the isolation level to {value} when it was already set to {this._isolationLevel}.");
				this._isolationLevel = value;
			}
		}
		private System.Data.IsolationLevel? _isolationLevel;

		internal UnitOfWorkMode Mode { get; private set; }

		/// <summary>
		/// Promotes the <see cref="UnitOfWork"/> to one of the given <see cref="UnitOfWorkMode"/>.
		/// Does nothing if it is already of that <see cref="UnitOfWorkMode"/>.
		/// Throws if the transition is an unsupported one.
		/// </summary>
		internal void PromoteToMode(UnitOfWorkMode mode)
		{
			this.Mode = (this.Mode, mode) switch
			{
				(UnitOfWorkMode.Manual, UnitOfWorkMode.ScopedExecution) => UnitOfWorkMode.ScopedExecution,
				var (oldValue, newValue) when oldValue == newValue => newValue, // Unchanged
				_ => throw new NotSupportedException($"A {nameof(UnitOfWork)} transition from {nameof(this.Mode)} {this.Mode} to {mode} is unsupported."),
			};
		}

		internal abstract bool TryStartTransaction();
		internal abstract Task<bool> TryStartTransactionAsync(CancellationToken cancellationToken);
		internal abstract Task<bool> TryStartTransactionAsync(bool async, CancellationToken cancellationToken);

		internal abstract bool TryCommitTransaction();
		internal abstract Task<bool> TryCommitTransactionAsync(CancellationToken cancellationToken);
		internal abstract Task<bool> TryCommitTransactionAsync(bool async, CancellationToken cancellationToken);

		internal abstract bool TryRollBackTransaction();
		internal abstract Task<bool> TryRollBackTransactionAsync(CancellationToken cancellationToken);
		internal abstract Task<bool> TryRollBackTransactionAsync(bool async, CancellationToken cancellationToken);

		/// <summary>
		/// <para>
		/// Invalidates the current <see cref="UnitOfWork"/> such that attempts to use the <see cref="DbContext"/> result in a <see cref="TransactionAbortedException"/>.
		/// </para>
		/// <para>
		/// If there is an ongoing transaction, this method rolls it back and returns true. Otherwise, it returns false.
		/// </para>
		/// </summary>
		internal abstract bool TryRollBackTransactionAndInvalidate();
		/// <summary>
		/// <para>
		/// Invalidates the current <see cref="UnitOfWork"/> such that attempts to use the <see cref="DbContext"/> result in a <see cref="TransactionAbortedException"/>.
		/// </para>
		/// <para>
		/// If there is an ongoing transaction, this method rolls it back and returns true. Otherwise, it returns false.
		/// </para>
		/// </summary>
		internal abstract Task<bool> TryRollBackTransactionAndInvalidateAsync(CancellationToken cancellationToken);
		/// <summary>
		/// <para>
		/// Invalidates the current <see cref="UnitOfWork"/> such that attempts to use the <see cref="DbContext"/> result in a <see cref="TransactionAbortedException"/>.
		/// </para>
		/// <para>
		/// If there is an ongoing transaction, this method rolls it back and returns true. Otherwise, it returns false.
		/// </para>
		/// </summary>
		internal abstract Task<bool> TryRollBackTransactionAndInvalidateAsync(bool async, CancellationToken cancellationToken);

		/// <summary>
		/// If the current <see cref="UnitOfWork"/> has been invalidated by a variant of <see cref="TryRollBackTransactionAndInvalidate"/>, this method makes it valid and usable again.
		/// </summary>
		internal abstract void UndoInvalidation();
	}

	/// <summary>
	/// <para>
	/// The unit of work shared by a scope and its effective parents and children.
	/// </para>
	/// <para>
	/// The unit of work's lifetime matches that of the effective outermost scope.
	/// </para>
	/// </summary>
	internal sealed partial class UnitOfWork<TDbContext> : UnitOfWork, IAsyncDisposable, IDisposable
		where TDbContext : DbContext
	{
		// #TODO: Reconsider thread-safety and disposed object checks

		/// <summary>
		/// Lazily instantiated.
		/// </summary>
		public TDbContext DbContext => this._dbContext ??= this.AcquireDbContext();
		private TDbContext? _dbContext;

		/// <summary>
		/// Lazily instantiated.
		/// </summary>
		public override DbContextObserver DbContextObserver => this._dbContextObserver ??= this.AcquireDbContextObserver();
		public DbContextObserver? _dbContextObserver;

		private Func<TDbContext> DbContextFactory { get; }

		public UnitOfWork(Func<TDbContext> dbContextFactory)
		{
			this._lock = new UltralightLock<UnitOfWork<TDbContext>>(this, self => ref self._lock);

			this.DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
		}

		public void Dispose()
		{
			if (!this.TrySetDisposed())
				return; // Already disposed

			this.DisposeCore(async: false).RequireCompleted();
		}

		public ValueTask DisposeAsync()
		{
			if (!this.TrySetDisposed())
				return new ValueTask(Task.CompletedTask); // Already disposed

			return this.DisposeCore(async: true);
		}

		/// <summary>
		/// Will only be invoked once.
		/// </summary>
		private async ValueTask DisposeCore(bool async)
		{
			ImmutableList<Exception>? exceptions = null;

			if (this._dbContextObserver != null)
			{
				try
				{
					this._dbContextObserver.Dispose();
				}
				catch (Exception e)
				{
					exceptions = (exceptions ?? ImmutableList<Exception>.Empty).Add(e);
				}
				finally
				{
					this._dbContextObserver = null;
				}
			}

			if (this._dbContext != null)
			{
				try
				{
					// Disposes DbContext.Database.CurrentTransaction as well
					if (async) await this._dbContext.DisposeAsync().ConfigureAwait(false);
					else this._dbContext.Dispose();
				}
				catch (Exception e)
				{
					exceptions = (exceptions ?? ImmutableList<Exception>.Empty).Add(e);
				}
				finally
				{
					this._dbContext = null;
				}
			}

			if (exceptions != null) throw new AggregateException(exceptions);
		}

		/// <summary>
		/// Returns the value of the <see cref="_dbContext"/> field, or an instance newly assigned to it if it was null.
		/// </summary>
		private TDbContext AcquireDbContext()
		{
			using var exclusiveLock = this.GetLock();

			if (this._dbContext != null) return this._dbContext;

			// Invoke the factory
			var dbContext = this.DbContextFactory() ?? throw new ArgumentException("The injected factory produced a null DbContext.");

			return this._dbContext = dbContext;
		}

		/// <summary>
		/// Returns the value of the <see cref="_dbContextObserver"/> field, or an instance newly assigned to it if it was null.
		/// </summary>
		private DbContextObserver AcquireDbContextObserver()
		{
			var dbContext = this.DbContext; // May lock, so must be retrieved outside of our own lock

			using var exclusiveLock = this.GetLock();

			if (this._dbContextObserver != null) return this._dbContextObserver;

			var observer = new DbContextObserver(dbContext);

			try
			{
				// Ensure that we have a transaction when any changes are about to be saved
				observer.WillSaveChanges = this.TryStartTransactionAsync;

				return this._dbContextObserver = observer;
			}
			catch
			{
				observer.Dispose();
				throw;
			}
		}
	}
}
