using System;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// Provides access to the <typeparamref name="TDbContext"/> of the ambient <see cref="DbContextScope{TDbContext}"/>.
	/// </para>
	/// </summary>
	internal sealed class AmbientDbContextAccessor<TDbContext> : IDbContextAccessor<TDbContext>
		where TDbContext : DbContext
	{
		public bool HasDbContext => DbContextScope<TDbContext>.CurrentOrDefault != null;

		/// <summary>
		/// <para>
		/// Returns the currently accessible <typeparamref name="TDbContext"/>, or throws an <see cref="InvalidOperationException"/> if there is none.
		/// </para>
		/// <para>
		/// The returned instance must <strong>not</strong> be diposed by the caller.
		/// </para>
		/// </summary>
		public TDbContext CurrentDbContext => DbContextScope<TDbContext>.CurrentDbContext;
	}
}
