using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Architect.EntityFramework.DbContextManagement.Tests
{
	/// <summary>
	/// Helps pretend we have pooling, accessing a temporary, in-memory database while pretending to dispose connections in between operations.
	/// Also permits reuse between DbContext instances.
	/// </summary>
	internal sealed class UndisposableSqliteConnection : SqliteConnection
	{
		private bool MayDispose { get; set; }

		public UndisposableSqliteConnection(string connectionString)
			: base(connectionString)
		{

		}

		/// <summary>
		/// Actually disposes this object and the underlying DbConnection object.
		/// </summary>
		public void TrulyDispose()
		{
			this.MayDispose = true;
			this.Dispose();
		}

		public override void Close()
		{
			if (!this.MayDispose) return;
			base.Close();
		}
		public override Task CloseAsync()
		{
			this.Close();
			return Task.CompletedTask;
		}
	}
}
