using System;

namespace Architect.EntityFramework.DbContextManagement.Tests.Providers.DbContextProviderTests
{
	public abstract class DbContextProviderTestBase : IDisposable
	{
		/// <summary>
		/// The last created <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.
		/// </summary>
		private protected TestDbContext LastDbContext { get; private set; }

		private protected DbContextScopeOptionsBuilder OptionsBuilder { get; } = new DbContextScopeOptionsBuilder();

		private protected DbContextProvider<TestDbContext> Provider => this._provider ??=
			new RegularDbContextProvider<TestDbContext>(this.TestDbContextFactory, this.OptionsBuilder.Build());
		private DbContextProvider<TestDbContext> _provider;

		private protected TestDbContextFactory TestDbContextFactory { get; } = new TestDbContextFactory(new UndisposableSqliteConnection("Filename=:memory:"));

		public void Dispose()
		{
			this.TestDbContextFactory.Dispose();
		}
	}
}
