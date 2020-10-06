using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EntityFramework.DbContextManagement.Example.Example
{
	internal static class OrderDemo
	{
		public static DbConnection Connection { get; private set; }

		public static async Task Run()
		{
			var sqliteConnection = new SqliteConnection("Filename=:memory:");
			sqliteConnection.Open();

			Connection = sqliteConnection;

			var hostBuilder = new HostBuilder();

			hostBuilder.ConfigureServices(services =>
			{
				services.AddPooledDbContextFactory<ExampleDbContext>(opt => opt
					.UseSqlite(sqliteConnection)//D:/Temp/Temp.db;")
					.AddInterceptors(new DummyInterceptor()));
					//.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll));
					//.AddDbContextScope();
			});

			hostBuilder.ConfigureServices(services => services.AddDbContextScope<ExampleContext, ExampleDbContext>(opt => opt
				//.DbContextFactory(() => new ExampleDbContext(new DbContextOptionsBuilder<ExampleDbContext>().UseSqlite(sqliteConnection).Options))
				//.DefaultScopeOption(AmbientScopeOption.NoNesting)
				.ExecutionStrategyOptions(ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure)));

			hostBuilder.ConfigureServices(services => services.AddConcurrencyConflictDbContextProvider<ExampleContext, ExampleDbContext>());

			hostBuilder.ConfigureServices(services => services.AddSingleton<OrderApplicationService>().AddSingleton<OrderRepo>());

			using var host = hostBuilder.Build();

			var dbContextFactory = host.Services.GetRequiredService<IDbContextFactory<ExampleDbContext>>();
			using (var dbContext = dbContextFactory.CreateDbContext())
				dbContext.Database.EnsureCreated();

			var orderApplicationService = host.Services.GetRequiredService<OrderApplicationService>();

			await orderApplicationService.AddTwoOrdersAndRetrieveThem();
		}

		public class DummyInterceptor : SaveChangesInterceptor
		{

		}
	}
}
