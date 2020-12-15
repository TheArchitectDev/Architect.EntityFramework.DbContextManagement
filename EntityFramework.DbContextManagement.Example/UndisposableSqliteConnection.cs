using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Architect.EntityFramework.DbContextManagement.Example
{
	/// <summary>
	/// <para>
	/// In-memory SQLite databases are removed when the connection is closed.
	/// </para>
	/// <para>
	/// To provide a degree of persistence for the demo, this reusable connection does not close when it is disposed.
	/// </para>
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
