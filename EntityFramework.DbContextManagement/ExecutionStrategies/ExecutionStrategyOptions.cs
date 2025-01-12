using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// Options for the <see cref="IExecutionStrategy"/> when working with scoped execution methods, such as <see cref="IDbContextProvider{TContext}.ExecuteInDbContextScope"/>.
	/// </summary>
	[Flags]
	public enum ExecutionStrategyOptions
	{
		/// <summary>
		/// A value that has all options disabled.
		/// </summary>
		None = 0,

		/// <summary>
		/// <para>
		/// A flag that indicates that optimistic concurrency failures should lead to retry attempts of the operation.
		/// </para>
		/// <para>
		/// This is intended for use with <see cref="PropertyBuilder.IsRowVersion"/> or <see cref="PropertyBuilder.IsConcurrencyToken(Boolean)"/>.
		/// </para>
		/// <para>
		/// With this option, a <see cref="DbUpdateConcurrencyException"/> is treated as a reason to retry when working with scoped execution methods,
		/// such as <see cref="IDbContextProvider{TContext}.ExecuteInDbContextScope"/>.
		/// </para>
		/// <para>
		/// Since the format lends itself to the situation perfectly, this setting provides a zero-effort way of resolving optimistic concurrency conflicts.
		/// It also creates consistency between retries because of connection issues and retries because of concurrency conflicts.
		/// </para>
		/// </summary>
		RetryOnOptimisticConcurrencyFailure = 1 << 0,
	}
}
