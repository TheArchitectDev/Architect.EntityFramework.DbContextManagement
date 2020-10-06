using System;

namespace Architect.EntityFramework.DbContextManagement.Locking
{
	/// <summary>
	/// <para>
	/// An ultralight locking mechanism that throws on concurrency conflicts.
	/// </para>
	/// <para>
	/// Uncontested, this implementation takes about 69% of the time a regular lock would take.
	/// This implementation takes about 117% of the time taken by a manual implementation (which would clutter the code significantly).
	/// </para>
	/// <para>
	/// The implementation is completely allocation-free. It is heavily inlined, and strongly optimized for the uncontested case.
	/// </para>
	/// <para>
	/// Disposed object detection is included practically for free.
	/// </para>
	/// </summary>
	internal struct UltralightLock<T>
	{
		public delegate ref UltralightLock<T> LockGetter(T owner);

		public bool IsDisposed => this._value == Int32.MaxValue;
		public bool IsUnlocked => this._value == -1;

		internal int _value;

		internal T Owner { get; }
		internal LockGetter GetLock { get; }

		public UltralightLock(T owner, LockGetter getValue)
		{
			this._value = -1;

			this.Owner = owner;
			this.GetLock = getValue;
		}

		internal void Throw()
		{
			if (this.IsDisposed)
				throw new ObjectDisposedException(this.Owner?.GetType().Name);

			throw new InvalidOperationException("The lock was contested. This may indicate parallel use of a type that is not thread-safe.");
		}
	}
}
