using System;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// Provides access to an <typeparamref name="TDbContext"/>.
	/// </para>
	/// </summary>
	public interface IDbContextAccessor<TDbContext>
		where TDbContext : DbContext
	{
		/// <summary>
		/// Returns whether or not a <typeparamref name="TDbContext"/> is available.
		/// </summary>
		bool HasDbContext { get; }

		/// <summary>
		/// <para>
		/// Returns the currently accessible <typeparamref name="TDbContext"/>, or throws an <see cref="InvalidOperationException"/> if there is none.
		/// </para>
		/// <para>
		/// The returned instance must <strong>not</strong> be diposed by the caller.
		/// </para>
		/// </summary>
		TDbContext CurrentDbContext { get; }
	}
}
