﻿using Microsoft.EntityFrameworkCore;

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
				entity.Property(o => o.UpdateDateTime)
					.HasPrecision(3)
					.IsConcurrencyToken();
				entity.HasMany(o => o.Children);
			});

			modelBuilder.Entity<Child>(entity =>
			{
				entity.Property(c => c.Id)
					.ValueGeneratedNever();
				entity.HasKey(c => c.Id);
			});
		}
	}
}
