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
	internal sealed class RegularDbContextProvider<TDbContext> : DbContextProvider<TDbContext>, IInternalDbContextProvider<TDbContext>
		where TDbContext : DbContext
	{
		private IDbContextFactory<TDbContext> DbContextFactory { get; }

		public override DbContextScopeOptions Options { get; }

		public RegularDbContextProvider(IDbContextFactory<TDbContext> dbContextFactory, DbContextScopeOptions? options = null)
		{
			this.DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));

			this.Options = options ?? DbContextScopeOptions.Default;
		}

		public override DbContextScope CreateDbContextScope(AmbientScopeOption? scopeOption = null)
		{
			var dbContextScope = DbContextScope.Create(this.DbContextFactory, scopeOption ?? this.Options.DefaultScopeOption);
			return dbContextScope;
		}
	}
}
