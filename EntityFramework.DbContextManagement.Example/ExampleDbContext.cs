using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.Example
{
	internal class ExampleDbContext : DbContext
	{
		public DbSet<Order> Orders { get; private set; }

		public ExampleDbContext(DbContextOptions<ExampleDbContext> options)
			: base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<Order>(entity =>
			{
				entity.Property(e => e.Id)
					.ValueGeneratedNever();

				entity.Property(e => e.Name)
					.IsRequired()
					.HasMaxLength(100);

				entity.HasKey(e => e.Id);

				entity.HasIndex(e => e.Name);
			});

			this.SeedOrders(modelBuilder);
		}

		/// <summary>
		/// Provides some initial data, for demo purposes.
		/// </summary>
		private void SeedOrders(ModelBuilder modelBuilder)
		{
			var orders = new[]
			{
				new Order(id: 1, name: "InitialOrder"),
			};

			modelBuilder.Entity<Order>().HasData(orders);
		}
	}
}
