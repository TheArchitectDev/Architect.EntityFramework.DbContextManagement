using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.Tests
{
	internal class TestDbContext : DbContext
	{
		public static TestDbContext Create()
		{
			return new TestDbContext();
		}

		public TestDbContext()
			: base(new DbContextOptionsBuilder().UseSqlite("Filename=:memory:").Options)
		{
		}

		public TestDbContext(DbContextOptions options)
			: base(options)
		{

		}
	}
}
