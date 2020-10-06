using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EntityFramework.DbContextManagement.Example
{
	public class ExampleContext
	{

	}

	internal class ExampleDbContext : DbContext
	{
		public DbSet<Order> Orders { get; private set; }
		public DbSet<Child> Children { get; private set; }

		public ExampleDbContext(DbContextOptions<ExampleDbContext> options)
			: base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<Order>(entity =>
			{
				entity.Property(o => o.UpdateDateTime).IsConcurrencyToken();
				entity.HasMany(o => o.Children);
			});

			modelBuilder.Entity<Child>(entity =>
			{
				entity.Property(c => c.Id)
					.ValueGeneratedNever();
				entity.HasKey(c => c.Id);
			});
		}

		//public override int SaveChanges()
		//{
		//	System.Console.WriteLine("Saving changes!");
		//	return base.SaveChanges();
		//}

		//public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
		//{
		//	System.Console.WriteLine("Original savechangesasync.");
		//	return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
		//}
	}
}
