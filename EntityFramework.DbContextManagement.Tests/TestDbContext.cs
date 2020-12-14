using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.Tests
{
	internal class TestDbContext : DbContext
	{
		public static TestDbContext Create()
		{
			return new TestDbContext();
		}
	}
}
