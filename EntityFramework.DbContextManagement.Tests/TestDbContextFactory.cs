using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.Tests
{
	internal class TestDbContextFactory : IDbContextFactory<TestDbContext>, IDisposable
	{
		private string UniqueName { get; } = Guid.NewGuid().ToString("N");
		private string ConnectionString => $"DataSource={this.UniqueName};Mode=Memory;Cache=Shared;";

		private DbConnection KeepAliveConnection { get; }

		public TestDbContextFactory()
		{
			this.KeepAliveConnection = this.CreateConnection();
			this.KeepAliveConnection.Open();

			using var context = TestDbContext.Create(this.ConnectionString);

			context.Database.EnsureCreated();
		}

		public void Dispose()
		{
			this.KeepAliveConnection.Dispose();
		}

		/// <summary>
		/// Attempts to reset the database by closing (and then reopening) the <see cref="KeepAliveConnection"/>.
		/// This only works if all other connections to the same database are closed as well.
		/// </summary>
		public void ResetDatabase()
		{
			this.KeepAliveConnection.Close();
			this.KeepAliveConnection.Open();
		}

		public TestDbContext CreateDbContext()
		{
			var result = TestDbContext.Create(this.ConnectionString);
			return result;
		}

		private DbConnection CreateConnection()
		{
			var result = new SqliteConnection(this.ConnectionString);
			return result;
		}
	}
}
