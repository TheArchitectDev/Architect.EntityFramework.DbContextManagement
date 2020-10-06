using System;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement.Providers;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// Provides an ambient <see cref="DbContextScope"/>, code in whose scope can access the <see cref="DbContext"/> through <see cref="IDbContextAccessor{TDbContext}"/>.
	/// </para>
	/// </summary>
	internal sealed class DbContextProvider<TDbContext> : IDbContextProvider<TDbContext>, IInternalDbContextProvider<TDbContext>
		where TDbContext : DbContext
	{
		private IDbContextFactory<TDbContext> DbContextFactory { get; }

		public DbContextScopeOptions Options { get; }

		public DbContextProvider(IDbContextFactory<TDbContext> dbContextFactory, DbContextScopeOptions? options = null)
		{
			this.DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));

			this.Options = options ?? DbContextScopeOptions.Default;
		}

		public DbContextScope CreateDbContextScope(AmbientScopeOption? scopeOption = null)
		{
			var dbContextScope = DbContextScope.Create(this.DbContextFactory, scopeOption ?? this.Options.DefaultScopeOption);
			return dbContextScope;
		}
	}
}
