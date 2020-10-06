using System;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// An <see cref="IDbContextAccessor{TDbContext}"/> implementation that uses a fixed <see cref="DbContext"/> instance.
	/// </para>
	/// <para>
	/// This non-generic type provides static Create methods for the generic type, to provide type inference.
	/// </para>
	/// </summary>
	public static class FixedDbContextAccessor
	{
		/// <summary>
		/// Returns an instance that uses the given <typeparamref name="TDbContext"/>.
		/// </summary>
		public static FixedDbContextAccessor<TDbContext> Create<TDbContext>(TDbContext dbContext)
			where TDbContext : DbContext
		{
			return Create(() => dbContext);
		}

		/// <summary>
		/// Returns an instance that uses the given <paramref name="dbContextFactory"/> on the first invocation, and returns the resulting <typeparamref name="TDbContext"/> from then on.
		/// </summary>
		public static FixedDbContextAccessor<TDbContext> Create<TDbContext>(Func<TDbContext> dbContextFactory)
			where TDbContext : DbContext
		{
			return new FixedDbContextAccessor<TDbContext>(dbContextFactory);
		}
	}

	/// <summary>
	/// An <see cref="IDbContextAccessor{TDbContext}"/> implementation that uses a fixed <see cref="Microsoft.EntityFrameworkCore.DbContext"/> instance.
	/// </summary>
	public sealed class FixedDbContextAccessor<TDbContext> : IDbContextAccessor<TDbContext>
		where TDbContext : DbContext
	{
		public bool HasDbContext => true;
		public TDbContext CurrentDbContext => this.DbContext ??= this.DbContextFactory!() ?? throw new Exception("The factory produced a null DbContext.");

		private TDbContext? DbContext { get; set; }
		private Func<TDbContext>? DbContextFactory { get; }

		/// <summary>
		/// Constructs an instance that uses the given <typeparamref name="TDbContext"/>.
		/// </summary>
		public FixedDbContextAccessor(TDbContext dbContext)
		{
			this.DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		}

		/// <summary>
		/// Constructs an instance that uses the given <paramref name="dbContextFactory"/> on the first invocation, and returns the resulting <typeparamref name="TDbContext"/> from then on.
		/// </summary>
		public FixedDbContextAccessor(Func<TDbContext> dbContextFactory)
		{
			this.DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
		}
	}
}
