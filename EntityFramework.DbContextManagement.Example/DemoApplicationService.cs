using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.Example
{
	internal class DemoApplicationService
	{
		private IDbContextProvider<ExampleDbContext> DbContextProvider { get; }
		private OrderRepository OrderRepository { get; }

		/// <summary>
		/// This service in the orchestrating layer has an <see cref="IDbContextProvider{TDbContext}"/> injected.
		/// This lets it provide a <see cref="DbContext"/> during each use case, with precise control over its scope and lifetime.
		/// </summary>
		public DemoApplicationService(IDbContextProvider<ExampleDbContext> dbContextProvider,
			OrderRepository orderRepository)
		{
			this.DbContextProvider = dbContextProvider ?? throw new ArgumentNullException(nameof(dbContextProvider));
			this.OrderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
		}

		public Task PrintOrderById(int id)
		{
			return this.DbContextProvider.ExecuteInDbContextScopeAsync(async _ =>
			{
				var order = await this.OrderRepository.LoadById(id);

				if (order is null) throw new KeyNotFoundException($"Order {id} was not found.");

				Console.WriteLine($"Printing {order}.");
				Console.WriteLine();
			});
		}

		public Task CreateOrder(int id, string name)
		{
			var order = new Order(id, name);

			return this.DbContextProvider.ExecuteInDbContextScopeAsync(async scope =>
			{
				var existingOrder = await this.OrderRepository.LoadById(id);
				
				if (existingOrder != null) throw new DuplicateNameException($"Order {id} already exists.");

				// Demonstrate a custom query
				// A transaction is automatically started here, since a custom query might make changes
				await this.OrderRepository.ClaimId(id);

				await this.OrderRepository.Add(order);

				await scope.DbContext.SaveChangesAsync();

				Console.WriteLine($"Created {order}.");
				Console.WriteLine();
			}); // The transaction is committed when the scope ends (unless scope.Abort() was called or an exception was uncaught)
		}

		public Task RenameOrder(int id, string newName)
		{
			return this.DbContextProvider.ExecuteInDbContextScopeAsync(async scope =>
			{
				var order = await this.OrderRepository.LoadById(id);

				if (order is null) throw new KeyNotFoundException($"Order {id} was not found.");

				// Invoke business logic
				order.Rename(newName);

				await this.OrderRepository.Update(order);

				// A transaction is automatically started here
				await scope.DbContext.SaveChangesAsync();

				Console.WriteLine($@"Renamed order {order.Id} to ""{order.Name}"".");
				Console.WriteLine();
			}); // The transaction is committed when the scope ends (unless scope.Abort() was called or an exception was uncaught)
		}
	}
}
