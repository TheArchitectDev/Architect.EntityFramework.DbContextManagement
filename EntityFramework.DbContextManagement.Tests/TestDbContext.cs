using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.Tests
{
	internal class TestDbContext : DbContext
	{
		public DbSet<TestEntity> TestEntities { get; private set; }

		public static TestDbContext Create()
		{
			var result = new TestDbContext("Filename=:memory:");
			return result;
		}

		public static TestDbContext Create(string connectionString)
		{
			var result = new TestDbContext(connectionString);
			return result;
		}

		private TestDbContext(string connectionString)
			: base(new DbContextOptionsBuilder().UseSqlite(connectionString).Options)
		{
		}
	}

	internal class TestEntity
	{
		[Key]
		public int Id { get; set; }
	}
}
