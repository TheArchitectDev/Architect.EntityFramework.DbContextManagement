using System;
using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// Used when no <see cref="IDbContextFactory{TContext}"/> was registered with Entity Framework.
	/// </para>
	/// <para>
	/// This class abstracts away the scenario where the <see cref="DbContext"/> is retrieved directly from the <see cref="IServiceProvider"/>.
	/// </para>
	/// </summary>
	internal sealed class DbContextFactory<TDbContext> : IDbContextFactory<TDbContext>
		where TDbContext : DbContext
	{
		public IServiceProvider ServiceProvider { get; }
		private Func<IServiceProvider, TDbContext> FactoryMethod { get; }

		public DbContextFactory(IServiceProvider serviceProvider, Func<IServiceProvider, TDbContext> factoryMethod)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			this.FactoryMethod = factoryMethod ?? throw new ArgumentNullException(nameof(factoryMethod));
		}

		public DbContextFactory(Func<TDbContext> factoryMethod)
		{
			if (factoryMethod is null) throw new ArgumentNullException(nameof(factoryMethod));

			this.ServiceProvider = null!;
			this.FactoryMethod = _ => factoryMethod();
		}

		public DbContextFactory(TDbContext fixedDbContext)
		{
			if (fixedDbContext is null) throw new ArgumentNullException(nameof(fixedDbContext));

			this.ServiceProvider = null!;
			this.FactoryMethod = _ => fixedDbContext;
		}

		public TDbContext CreateDbContext()
		{
			var dbContext = this.FactoryMethod(this.ServiceProvider) ?? throw new Exception($"The factory provided a null {typeof(TDbContext).Name}.");
			return dbContext;
		}
	}
}
