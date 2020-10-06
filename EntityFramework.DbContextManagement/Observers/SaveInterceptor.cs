using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Architect.EntityFramework.DbContextManagement.Observers
{
	internal sealed class SaveInterceptor : SaveChangesInterceptor
	{
		private Func<bool, CancellationToken, Task> WillSaveChanges { get; }
		private Action DidSaveChanges { get; }

		public SaveInterceptor(Func<bool, CancellationToken, Task> willSaveChanges, Action didSaveChanges)
		{
			Debug.Assert(willSaveChanges != null);
			Debug.Assert(didSaveChanges != null);

			this.WillSaveChanges = willSaveChanges;
			this.DidSaveChanges = didSaveChanges;
		}

		public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
		{
			this.WillSaveChanges(false, default);

			return base.SavingChanges(eventData, result);
		}

		public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
		{
			await this.WillSaveChanges(true, cancellationToken);

			return await base.SavingChangesAsync(eventData, result, cancellationToken);
		}

		public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
		{
			this.DidSaveChanges();

			return base.SavedChanges(eventData, result);
		}
	}
}
