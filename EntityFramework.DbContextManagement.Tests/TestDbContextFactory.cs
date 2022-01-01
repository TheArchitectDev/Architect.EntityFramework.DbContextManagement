using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.Tests
{
	internal class TestDbContextFactory : IDbContextFactory<TestDbContext>, IDisposable
	{
		public UndisposableSqliteConnection Connection { get; }

		public TestDbContextFactory(UndisposableSqliteConnection connection)
		{
			this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));

			using var context = TestDbContext.Create(connection);

			context.Database.EnsureCreated();
		}

		public void Dispose()
		{
			this.Connection.TrulyDispose();
		}

		public TestDbContext CreateDbContext()
		{
			var result = TestDbContext.Create(this.Connection);
			return result;
		}
	}
}
