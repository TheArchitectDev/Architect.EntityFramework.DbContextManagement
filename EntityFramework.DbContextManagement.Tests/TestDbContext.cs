using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.Tests
{
	internal class TestDbContext : DbContext
	{
		public DbSet<TestEntity> TestEntities { get; private set; }

		public static TestDbContext Create()
		{
			var result = new TestDbContext();
			return result;
		}

		public static TestDbContext Create(DbConnection dbConnection)
		{
			var result = new TestDbContext(dbConnection);
			return result;
		}

		private TestDbContext()
			: base(new DbContextOptionsBuilder().UseSqlite("Filename=:memory:").Options)
		{
		}

		private TestDbContext(DbConnection dbConnection)
			: base(new DbContextOptionsBuilder().UseSqlite(dbConnection).Options)
		{
		}
	}

	internal class TestEntity
	{
		[Key]
		public int Id { get; set; }
	}
}
