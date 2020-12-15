using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.Example
{
	internal class OrderRepository
	{
		/// <summary>
		/// This property gives easy access to the <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.
		/// It is always obtained from the <see cref="DbContextAccessor"/>.
		/// </summary>
		private ExampleDbContext DbContext => this.DbContextAccessor.CurrentDbContext;

		private IDbContextAccessor<ExampleDbContext> DbContextAccessor { get; }

		/// <summary>
		/// This repository in the infrastructure/persistence layer has an <see cref="IDbContextAccessor{TDbContext}"/> injected.
		/// This gives it access to the <see cref="Microsoft.EntityFrameworkCore.DbContext"/> provided and managed by a service higher up the call stack, usually in the orchestrating layer.
		/// </summary>
		public OrderRepository(IDbContextAccessor<ExampleDbContext> dbContextAccessor)
		{
			this.DbContextAccessor = dbContextAccessor ?? throw new ArgumentNullException(nameof(dbContextAccessor));
		}

		public Task<Order> LoadById(int id)
		{
			return this.DbContext.Orders
				.SingleOrDefaultAsync(order => order.Id == id);
		}

		/// <summary>
		/// <para>
		/// Claims the given ID. Once this succeeds, the current transaction can expect to be able to insert that ID with no further race conditions.
		/// </para>
		/// <para>
		/// This method is mainly used to demonstrate a custom query.
		/// </para>
		/// </summary>
		public Task ClaimId(int id)
		{
			return this.DbContext.Database.ExecuteSqlRawAsync(@"
INSERT INTO Orders (Id, Name)
VALUES
({0}, '');

DELETE FROM Orders
WHERE Id = {0};
", id);
		}

		public Task Add(Order order)
		{
			// AddAsync is for special value generators only, but we still keep this method Task-based for consistency and to keep our options open
			this.DbContext.Orders.Add(order);
			return Task.CompletedTask;
		}

		public Task Update(Order order)
		{
			// Update is synchronous, but we still keep this method Task-based for consistency and to keep our options open
			this.DbContext.Orders.Update(order);
			return Task.CompletedTask;
		}
	}
}
