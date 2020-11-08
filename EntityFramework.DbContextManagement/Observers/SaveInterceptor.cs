using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Architect.EntityFramework.DbContextManagement.Observers
{
	internal sealed class SaveInterceptor : SaveChangesInterceptor
	{
		private Func<bool, CancellationToken, Task> WillSaveChanges { get; }
		private Action<bool> DidSaveChanges { get; }

		public SaveInterceptor(Func<bool, CancellationToken, Task> willSaveChanges, Action<bool> didSaveChanges)
		{
			System.Diagnostics.Debug.Assert(willSaveChanges != null);
			System.Diagnostics.Debug.Assert(didSaveChanges != null);

			this.WillSaveChanges = willSaveChanges;
			this.DidSaveChanges = didSaveChanges;
		}

		public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
		{
			this.WillSaveChanges(false, default).RequireCompleted();

			return base.SavingChanges(eventData, result);
		}

		public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
		{
			await this.WillSaveChanges(true, cancellationToken).ConfigureAwait(false);

			return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
		}

		public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
		{
			this.DidSaveChanges(true);

			return base.SavedChanges(eventData, result);
		}

		public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
		{
			this.DidSaveChanges(true);

			return base.SavedChangesAsync(eventData, result, cancellationToken);
		}

		public override void SaveChangesFailed(DbContextErrorEventData eventData)
		{
			this.DidSaveChanges(false);

			base.SaveChangesFailed(eventData);
		}

		public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
		{
			this.DidSaveChanges(false);

			return base.SaveChangesFailedAsync(eventData, cancellationToken);
		}
	}
}
