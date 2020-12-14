namespace Architect.EntityFramework.DbContextManagement.Tests.Providers.DbContextProviderTests
{
	public abstract class DbContextProviderTestBase
	{
		/// <summary>
		/// The last created <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.
		/// </summary>
		private protected TestDbContext LastDbContext { get; private set; }

		private protected DbContextScopeOptionsBuilder OptionsBuilder { get; } = new DbContextScopeOptionsBuilder();

		private protected DbContextProvider<TestDbContext> Provider => this._provider ??=
			new RegularDbContextProvider<TestDbContext>(new DbContextFactory<TestDbContext>(() => this.LastDbContext = TestDbContext.Create()), this.OptionsBuilder.Build());
		private DbContextProvider<TestDbContext> _provider;
	}
}
