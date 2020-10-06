using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// Controls the behavior of flushing to the database when a query is performed while there are unsaved changes.
	/// </para>
	/// <para>
	/// Applies when working with scoped execution methods, such as <see cref="IDbContextProvider{TContext}.ExecuteInDbContextScope"/>.
	/// </para>
	/// </summary>
	public enum AutoFlushMode : byte
	{
		/// <summary>
		/// <para>
		/// Applies when working with scoped execution methods, such as <see cref="IDbContextProvider{TContext}.ExecuteInDbContextScope"/>.
		/// </para>
		/// <para>
		/// Auto-flush is disabled.
		/// No changes are observed.
		/// </para>
		/// <para>
		/// If a query is executed while there are unsaved changes, the database will not reflect the unsaved changes.
		/// </para>
		/// <para>
		/// This option offers the best performance. It is suitable if you are certain never to depend on unsaved changes when performing a query.
		/// </para>
		/// </summary>
		None = 0,

		// #TODO: Does this also ignore child set additions and removal? (And does child set removal get properly detected without a call to Remove()?)
		/// <summary>
		/// <para>
		/// Applies when working with scoped execution methods, such as <see cref="IDbContextProvider{TContext}.ExecuteInDbContextScope"/>.
		/// </para>
		/// <para>
		/// Auto-flush is enabled.
		/// Only explicit changes are observed, by methods such as <see cref="DbSet{TEntity}.Add(TEntity)"/>, <see cref="DbSet{TEntity}.Update"/>, and <see cref="DbSet{TEntity}.Remove(TEntity)"/>.
		/// </para>
		/// <para>
		/// If a query is executed while there are such unsaved changes, the changes are saved (but not committed) to the database just before the query is executed.
		/// </para>
		/// <para>
		/// Note that if there are <strong>only</strong> implicit changes, such as property modifications without a call to <see cref="DbSet{TEntity}.Update"/>,
		/// the changes are not observed, and the database will not reflect them.
		/// </para>
		/// <para>
		/// This option may offer improved performance compared to <see cref="DetectExplicitAndImplicitChanges"/>.
		/// It is suitable if all changes are marked by an explicit call to <see cref="DbSet{TEntity}.Add(TEntity)"/>, <see cref="DbSet{TEntity}.Update(TEntity)"/>, etc.
		/// </para>
		/// </summary>
		DetectExplicitChanges = 1,

		/// <summary>
		/// <para>
		/// Applies when working with scoped execution methods, such as <see cref="IDbContextProvider{TContext}.ExecuteInDbContextScope"/>.
		/// </para>
		/// <para>
		/// Auto-flush is enabled.
		/// All changes are observed.
		/// </para>
		/// <para>
		/// If a query is executed while there are unsaved changes, the changes are saved (but not committed) to the database just before the query is executed.
		/// </para>
		/// <para>
		/// This option provides the safest and most intuitive behavior.
		/// It comes at the cost of potential calls to <see cref="ChangeTracker.HasChanges()"/> (which may be affected by the value of <see cref="ChangeTracker.AutoDetectChangesEnabled"/>).
		/// </para>
		/// </summary>
		DetectExplicitAndImplicitChanges = 2,
	}
}
