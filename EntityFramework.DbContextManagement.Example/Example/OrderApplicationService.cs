using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFramework.DbContextManagement.Example.Example
{
	internal sealed class OrderApplicationService : IAsyncDisposable
	{
		//private IServiceProvider ServiceProvider { get; }
		private IDbContextProvider<ExampleDbContext> DbContextProvider { get; }
		private OrderRepo OrderRepo { get; }

		//private DbContextScope<ExampleDbContext> Scope { get; }

		public OrderApplicationService(/*IServiceProvider serviceProvider, */IDbContextProvider<ExampleDbContext> dbContextProvider, OrderRepo orderRepo)
		{
			//this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			this.DbContextProvider = dbContextProvider ?? throw new ArgumentNullException(nameof(dbContextProvider));
			this.OrderRepo = orderRepo ?? throw new ArgumentNullException(nameof(orderRepo));

			//this.Scope = this.DbContextProvider.CreateDbContextScope();
		}

		public ValueTask DisposeAsync()
		{
			return new ValueTask();
			//await this.Scope.DisposeAsync();
		}

		public async Task ThrowBecauseNoDbContext()
		{
			// Should throw because we created no scope
			await this.OrderRepo.GetAllOrders();
		}

		public async Task<IReadOnlyCollection<Order>> GetAllOrders()
		{
			//await using var dbContextScope = this.DbContextProvider.CreateDbContextScope();

			var orders = await this.OrderRepo.GetAllOrders();
			return orders;
		}

		public async Task<IReadOnlyCollection<Order>> SaveOrder()
		{
			//await using var dbContextScope = this.DbContextProvider.CreateDbContextScope();

			var orders = await this.OrderRepo.GetAllOrders();

			var order = orders.First();
			order.Name = "Henk";

			await this.OrderRepo.AddOrder(order);

			//await this.Scope.SaveChangesAsync();

			return orders;
		}

		public async Task AddTwoOrdersAndRetrieveThem()
		{
			IDbContextProvider<ExampleContext> prov = new MockDbContextProvider<ExampleContext, ExampleDbContext>(
				new DbContextScopeOptionsBuilder(){ ExecutionStrategyOptions = ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure }.Build())//() => this.ServiceProvider.GetRequiredService<IDbContextFactory<ExampleDbContext>>().CreateDbContext());
			{
				ScopedExecutionThrowsConcurrencyException = true,
			};

			//var appServ = new OrderApplicationService(this.ServiceProvider, prov, this.OrderRepo));

			try
			{
				await prov.ExecuteInDbContextScopeAsync(task: async (executionScope) =>
				{
					executionScope.IsolationLevel = System.Data.IsolationLevel.ReadCommitted;

					await prov.ExecuteInDbContextScopeAsync(async e => { await Task.Delay(0); e.Abort(); });
					await Task.Delay(0);
					var scopie = DbContextScope<ExampleDbContext>.Current;
					var contextie = DbContextScope<ExampleDbContext>.CurrentDbContext;
					throw new TimeoutException();
				});
			}
			catch (TimeoutException)
			{

			}

			// It even works with multiple instances in a scope :)
			//using (var scope = this.ServiceProvider.CreateScope())
			//{
			//	Parallel.For(0, 2, async i =>
			//	{
			//		if (i == 1) await Task.Delay(1000);
			//		await scope.ServiceProvider.GetRequiredService<IDbContextProvider<ExampleDbContext>>().ExecuteInDbContextScopeAsync(async () =>
			//		{
			//			await Task.Delay(10000);
			//		});
			//	});

			//	await Task.Delay(100000);
			//}

			await this.DbContextProvider.ExecuteInDbContextScopeAsync(async executionScope =>
			{
				executionScope.IsolationLevel = System.Data.IsolationLevel.Serializable;

				await Task.Delay(0);
				var dbContext = DbContextScope<ExampleDbContext>.Current.DbContext;
				var connection = dbContext.Database.GetDbConnection();
				//dbContext.Database.SetDbConnection(null);
				//dbContext.Dispose();
				Console.WriteLine();
			});

			//await this.DbContextProvider.ExecuteInDbContextScopeAsync(async () =>
			//{
			//	var dbContextScope = DbContextScope<ExampleDbContext>.Current;

			//	// For SQLite (not normally needed, let alone here)
			//	dbContextScope.DbContext.Database.OpenConnection();
			//	dbContextScope.DbContext.Database.EnsureCreated();

			//	var order = new Order() { Id = 1, Name = "Order" };
			//	var one = new Child(1, "One");
			//	order.AddChild(one);

			//	dbContextScope.DbContext.Orders.Add(order);
			//	dbContextScope.DbContext.SaveChanges();
			//});

			//await this.DbContextProvider.ExecuteInDbContextScopeAsync(async () =>
			//{
			//	var dbContextScope = DbContextScope<ExampleDbContext>.Current;

			//	dbContextScope.DbContext.Database.OpenConnection();

			//	var count = dbContextScope.DbContext.Orders.Count();
			//});
			//return;

			await this.DbContextProvider.ExecuteInDbContextScopeAsync(async executionScope =>
			{

				//await this.DbContextProvider.ExecuteInDbContextScopeAsync(async executionScope =>
				//{
				//	executionScope.IsolationLevel = System.Data.IsolationLevel.ReadCommitted;
				//	//DbContextScope<ExampleDbContext>.Current.DbContext.SaveChanges();
				//	await Task.Delay(0);
				//	//executionScope.Complete();
				//	//executionScope.Abort();
				//});

				executionScope.IsolationLevel = System.Data.IsolationLevel.ReadCommitted;
				var dbContextScope = DbContextScope<ExampleDbContext>.Current;

				//dbContextScope.DbContext.SaveChanges();

				var order = new Order() { Id = 1, Name = "Order" };
				var one = new Child(1, "One");
				order.AddChild(one);

				dbContextScope.DbContext.Orders.Add(order);
				//var count = dbContextScope.DbContext.Orders.Count();

				//await this.DbContextProvider.ExecuteInDbContextScopeAsync(async () => {
				//	await Task.Delay(0);
				//	DbContextScope<ExampleDbContext>.Current.DbContext.SaveChanges();
				//	DbContextScope<ExampleDbContext>.Current.Complete();
				//});

				//dbContextScope.DbContext.Entry(order).State = EntityState.Detached;
				//dbContextScope.DbContext.Entry(one).State = EntityState.Detached;

				var affected = dbContextScope.DbContext.Database.ExecuteSqlRaw("UPDATE Orders SET UpdateDateTime = date('now')");
				if (affected != 0) throw new Exception();

				dbContextScope.DbContext.SaveChanges();

				affected = dbContextScope.DbContext.Database.ExecuteSqlRaw("UPDATE Orders SET UpdateDateTime = date('now')"); // Will only be seen once entity is detached
				if (affected != 1) throw new Exception();
				//dbContextScope.DbContext.Entry(order).State = EntityState.Detached;

				var reloaded = dbContextScope.DbContext.Orders.Include(o => o.Children).Single();
				//var reloaded = order;

				reloaded.Name = "Henkieeee";
				dbContextScope.DbContext.Database.ExecuteSqlRaw("SELECT date('now');");
				
				reloaded.Children.First().Name = "CHANGED";
				var two = new Child(2, "Two"); // Code-generated ID issue!!
				reloaded.AddChild(two);

				//dbContextScope.DbContext.Update(reloaded);

				var state = dbContextScope.DbContext.Entry(two).State;

				await dbContextScope.DbContext.SaveChangesAsync();

				dbContextScope.DbContext.Entry(one).State = EntityState.Detached;
				dbContextScope.DbContext.Entry(two).State = EntityState.Detached;
				dbContextScope.DbContext.Entry(order).State = EntityState.Detached;

				var reloaded2 = dbContextScope.DbContext.Orders.Join(dbContextScope.DbContext.Children, o => o.Id, c => c.OrderId, (o, c) => new { o, c }).ToList();

				//executionScope.Complete();

				Console.WriteLine();
			});

			await this.DbContextProvider.ExecuteInDbContextScopeAsync(Execute);

			async Task Execute(IExecutionScope executionScope)
			{
				var dbContextScope = DbContextScope<ExampleDbContext>.Current;
				dbContextScope.DbContext.ChangeTracker.Tracked += (a, b) => Console.WriteLine(b.Entry + ": " + b.FromQuery);

				// For SQLite (not normally needed, let alone here)
				dbContextScope.DbContext.Database.OpenConnection();
				dbContextScope.DbContext.Database.EnsureCreated();

				var one = new Order() { Id = 1, Name = "One", UpdateDateTime = new DateTime(2020, 01, 01) };
				var two = new Order() { Id = 2, Name = "Two" };

				await this.OrderRepo.AddOrder(one);
				await this.OrderRepo.AddOrder(two);

				executionScope.DbContext.SaveChanges();

				var loaded = dbContextScope.DbContext.Orders.First(); // Triggers save due to earlier adding

				// #TODO: Test which properties get saved to the database if you use Update(). Answer this both with and without Tracking.
				loaded.Name += "HI";

				dbContextScope.DbContext.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM Orders"); // Triggers save due to earlier property modification
				var orders1 = await this.OrderRepo.GetAllOrders();
				//await dbContextScope.SaveChangesAsync();
				var orders2 = await this.OrderRepo.GetAllOrders();

				await this.DbContextProvider.ExecuteInDbContextScopeAsync(AmbientScopeOption.JoinExisting, default, async (executionScope, ct) =>
				{
					//await using (var nestedDbContextScope = this.DbContextProvider.CreateDbContextScope(AmbientScopeOption.JoinExisting))
					//{
					//	var orders = await this.OrderRepo.GetAllOrders();
					//	if (orders.Count != 0) throw new Exception("Unexpectedly saw result of unsaved changes.");

					//	// Indicate that we have no intention to roll back
					//	await nestedDbContextScope.SaveChangesAsync();
					//}

					//await dbContextScope.SaveChangesAsync();

					{
						var orders = await this.OrderRepo.GetAllOrders();
						if (orders.Count != 2) throw new Exception("Unexpected result.");
						var loadedOne = orders.First();
						var loadedTwo = orders.Last();

						if (!ReferenceEquals(loadedOne, one)) throw new Exception("Identity map did not work for order One. Most likely, multiple DbContexts were used.");
						if (!ReferenceEquals(loadedTwo, two)) throw new Exception("Identity map did not work for order Two. Most likely, multiple DbContexts were used.");
					}

					await using (var nestedDbContextScope = this.DbContextProvider.CreateDbContextScope(AmbientScopeOption.JoinExisting))
					{
						var orders = await this.OrderRepo.GetAllOrders();
						if (orders.Count != 2) throw new Exception("Unexpected result.");
						var loadedOne = orders.First();
						var loadedTwo = orders.Last();

						if (!ReferenceEquals(loadedOne, one)) throw new Exception("Identity map did not work for order One. Most likely, multiple DbContexts were used.");
						if (!ReferenceEquals(loadedTwo, two)) throw new Exception("Identity map did not work for order Two. Most likely, multiple DbContexts were used.");

						await nestedDbContextScope.DbContext.SaveChangesAsync();
					}

					await using (var unrelatedDbContextScope = this.DbContextProvider.CreateDbContextScope(AmbientScopeOption.ForceCreateNew))
					{
						if (DbContextScope<ExampleDbContext>.CurrentDbContext == dbContextScope.DbContext) throw new Exception("Expected different DbContext instances.");

						// For SQLite (not normally needed, let alone here)
						unrelatedDbContextScope.DbContext.Database.SetDbConnection(dbContextScope.DbContext.Database.GetDbConnection());

						var orders = await this.OrderRepo.GetAllOrders();
						if (orders.Count != 2) throw new Exception("Unexpected result.");
						var loadedOne = orders.First();
						var loadedTwo = orders.Last();

						if (ReferenceEquals(loadedOne, one)) throw new Exception("Loaded instance One should not have matched original. DbContexts should have been different.");
						if (ReferenceEquals(loadedTwo, two)) throw new Exception("Loaded instance Two should not have matched original. DbContexts should have been different.");
					}
				});
			}
		}
	}
}
