using System.Runtime.CompilerServices;
using Architect.EntityFramework.DbContextManagement.Locking;

namespace Architect.EntityFramework.DbContextManagement.DbContextScopes
{
	// This partial provides extremely lightweight locking, which throws if a lock is contested
	// The behavior includes disposed object detection practically for free
	internal sealed partial class UnitOfWork<TDbContext>
	{
		/// <summary>
		/// An ultralight locking mechanism that throws on concurrency conflicts.
		/// </summary>
		private UltralightLock<UnitOfWork<TDbContext>> _lock;

		/// <summary>
		/// <para>
		/// Returns a struct representing exclusive access to a lock.
		/// </para>
		/// <para>
		/// Throws if the lock is contended or if the current object was disposed. Both indicate developer error.
		/// </para>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private UltralightLockHandle<UnitOfWork<TDbContext>> GetLock()
		{
			return this._lock.Acquire();
		}

		private bool TrySetDisposed()
		{
			return this._lock.TryDispose();
		}
	}
}
