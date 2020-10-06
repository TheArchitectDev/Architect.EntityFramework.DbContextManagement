using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Architect.EntityFramework.DbContextManagement;
using Microsoft.EntityFrameworkCore;

namespace EntityFramework.DbContextManagement.Example.Example
{
	internal sealed class OrderRepo
	{
		private IDbContextAccessor<ExampleDbContext> DbContextAccessor { get; }
		private ExampleDbContext DbContext => this.DbContextAccessor.CurrentDbContext;

		public OrderRepo(IDbContextAccessor<ExampleDbContext> dbContextAccessor)
		{
			this.DbContextAccessor = dbContextAccessor ?? throw new ArgumentNullException(nameof(dbContextAccessor));
		}

		public Task AddOrder(Order order)
		{
			this.DbContext.Add(order);
			return Task.CompletedTask;
		}

		public async Task<IReadOnlyCollection<Order>> GetAllOrders()
		{
			return await this.DbContext.Orders.ToListAsync();
		}
	}
}
