using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.DbContextScopes
{
	internal sealed partial class UnitOfWork<TDbContext>
	{
		/// <summary>
		/// <para>
		/// Adds the following behavior to the current <see cref="DbContext"/>:
		/// If any non-save query is attempted while there are unsaved changes, <see cref="DbContext.SaveChanges()"/> is invoked first.
		/// </para>
		/// <para>
		/// The saving is, unfortunately, strictly synchronous.
		/// </para>
		/// </summary>
		internal override void TryAddAutoFlushBehavior()
		{
			var observer = this.DbContextObserver;

			using var exclusiveLock = this.GetLock();

			observer.WillPerformNonSaveQueryWithUnsavedChanges += this.SaveChangesBeforeDataMayBeLoaded;
		}

		/// <summary>
		/// <para>
		/// Removes the behavior resulting from <see cref="TryAddAutoFlushBehavior"/>.
		/// </para>
		/// </summary>
		internal override void TryRemoveAutoFlushBehavior()
		{
			if (this._dbContextObserver is null) return;

			var observer = this.DbContextObserver;

			using var exclusiveLock = this.GetLock();

			if (observer is null) return;

			observer.WillPerformNonSaveQueryWithUnsavedChanges -= this.SaveChangesBeforeDataMayBeLoaded;
		}

		private void SaveChangesBeforeDataMayBeLoaded()
		{
			// Save the changes (without committing) so that they are visible to any queries
			// (Note that we also have behavior that starts a transaction (if there is none) when data is about to be saved, which will be triggered by this)
			this.DbContext.SaveChanges();
		}
	}
}
