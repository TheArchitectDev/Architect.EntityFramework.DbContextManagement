using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Architect.EntityFramework.DbContextManagement.Example
{
	/// <summary>
	/// Demonstrates some uses of the EntityFramework.DbContextManagement package.
	/// </summary>
	internal static class Program
	{
		private static async Task Main()
		{
			var hostBuilder = new HostBuilder();
			var startup = new Startup();
			hostBuilder.ConfigureServices(startup.ConfigureServices);
			using var host = hostBuilder.Build();
			startup.Configure(host);

			// Seed the in-memory database
			{
				var dbContextFactory = host.Services.GetRequiredService<IDbContextFactory<ExampleDbContext>>();
				using var dbContext = dbContextFactory.CreateDbContext();
				await dbContext.Database.EnsureDeletedAsync();
				await dbContext.Database.EnsureCreatedAsync();
			}

			await host.StartAsync();

			var demoApplicationService = host.Services.GetRequiredService<DemoApplicationService>();

			await demoApplicationService.PrintOrderById(id: 1);
			await demoApplicationService.CreateOrder(id: 2, name: "NewlyCreatedOrder");
			await demoApplicationService.RenameOrder(id: 2, newName: "RenamedOrder");

			await host.StopAsync();
		}
	}
}
