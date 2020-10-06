using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Architect.EntityFramework.DbContextManagement.Locking
{
	/// <summary>
	/// <para>
	/// A handle that reprents unique access to a lock.
	/// </para>
	/// <para>
	/// Dispose to release.
	/// </para>
	/// </summary>
	internal readonly struct UltralightLockHandle<T> : IDisposable
	{
		private T Owner { get; }
		private UltralightLock<T>.LockGetter GetLock { get; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public UltralightLockHandle(ref UltralightLock<T> ultralightLock)
		{
			this.Owner = ultralightLock.Owner;
			this.GetLock = ultralightLock.GetLock;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			Interlocked.CompareExchange(ref this.GetLock(this.Owner)._value, value: -1, comparand: 1);
		}
	}
}
