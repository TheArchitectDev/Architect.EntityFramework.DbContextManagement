using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Architect.EntityFramework.DbContextManagement.Locking
{
	internal static class UltralightLockExtensions
	{
		/// <summary>
		/// <para>
		/// Acquires the lock, returning a lock handle on success.
		/// </para>
		/// <para>
		/// Throws if the lock is contested or if it is already disposed.
		/// </para>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UltralightLockHandle<T> Acquire<T>(this ref UltralightLock<T> ultralightLock)
		{
			if (Interlocked.CompareExchange(ref ultralightLock._value, value: 1, comparand: -1) != -1)
				ultralightLock.Throw();

			return new UltralightLockHandle<T>(ref ultralightLock);
		}

		/// <summary>
		/// Attempts to mark the <see cref="UltralightLock{T}"/> as disposed. Returns true on success or false if it was already marked as disposed.
		/// </summary>
		public static bool TryDispose<T>(this ref UltralightLock<T> ultralightLock)
		{
			return Interlocked.Exchange(ref ultralightLock._value, value: Int32.MaxValue) != Int32.MaxValue;
		}
	}
}
