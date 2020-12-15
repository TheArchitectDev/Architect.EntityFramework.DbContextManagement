using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Architect.EntityFramework.DbContextManagement.Example
{
	internal class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			// Register the DbContext, using the factory-based extension methods of EF Core 5+
			services.AddPooledDbContextFactory<ExampleDbContext>(context => context.UseSqlite(new UndisposableSqliteConnection("Filename=:memory:")));

			// Register IDbContextProvider<T> and IDbContextAccessor<T> for the DbContext
			services.AddDbContextScope<ExampleDbContext>();

			// Alternatively, if the DbContext cannot be registered using a *factory-based* extension method, provide a custom factory to AddDbContextScope
			//services.AddDbContextScope<ExampleDbContext>(scope =>
			//	scope.DbContextFactory(() => new ExampleDbContext(new DbContextOptionsBuilder<ExampleDbContext>().UseSqlite("Filename=:memory:").Options)));

			// Register the OrderRepository
			services.AddSingleton<OrderRepository>();

			// Register the demo application
			services.AddSingleton<DemoApplicationService>();
		}

		public void Configure(IHost _) // IHost for generic hosts, IApplicationBuilder for web hosts
		{
		}
	}
}
