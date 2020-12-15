using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Architect.EntityFramework.DbContextManagement.Tests.Providers.DbContextProviderTests.ScopedExecutionTests
{
	public class TransactionTests : ScopedExecutionTestBase
	{
		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithReadOnlyQueries_ShouldNotStartTransaction(Overload overload)
		{
			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				((TestDbContext)scope.DbContext).TestEntities.Count();
				((TestDbContext)scope.DbContext).TestEntities.FirstOrDefault();

				Assert.Null(scope.DbContext.Database.CurrentTransaction);

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithNeedlessSaveChanges_ShouldNotStartTransaction(Overload overload)
		{
			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				Assert.Null(scope.DbContext.Database.CurrentTransaction);

				scope.DbContext.SaveChanges();

				Assert.NotNull(scope.DbContext.Database.CurrentTransaction);

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithNeedlessSaveChangesAsync_ShouldNotStartTransaction(Overload overload)
		{
			await this.Execute(overload, this.Provider, async (scope, ct) =>
			{
				Assert.Null(scope.DbContext.Database.CurrentTransaction);

				await scope.DbContext.SaveChangesAsync();

				Assert.NotNull(scope.DbContext.Database.CurrentTransaction);

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithAsyncReadOnlyQueries_ShouldNotStartTransaction(Overload overload)
		{
			await this.Execute(overload, this.Provider, async (scope, ct) =>
			{
				await ((TestDbContext)scope.DbContext).TestEntities.CountAsync();
				await ((TestDbContext)scope.DbContext).TestEntities.FirstOrDefaultAsync();

				Assert.Null(scope.DbContext.Database.CurrentTransaction);

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithCustomQuery_ShouldStartTransaction(Overload overload)
		{
			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				Assert.Null(scope.DbContext.Database.CurrentTransaction);

				scope.DbContext.Database.ExecuteSqlRaw("SELECT sqlite_version();");

				Assert.NotNull(scope.DbContext.Database.CurrentTransaction);

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithAsyncCustomQuery_ShouldStartTransaction(Overload overload)
		{
			await this.Execute(overload, this.Provider, async (scope, ct) =>
			{
				Assert.Null(scope.DbContext.Database.CurrentTransaction);

				await scope.DbContext.Database.ExecuteSqlRawAsync("SELECT sqlite_version();");

				Assert.NotNull(scope.DbContext.Database.CurrentTransaction);

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithSaveChanges_ShouldStartTransaction(Overload overload)
		{
			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				Assert.Null(scope.DbContext.Database.CurrentTransaction);

				var entityToAdd = new TestEntity() { Id = 1 };
				scope.DbContext.Add(entityToAdd);
				scope.DbContext.SaveChanges();

				Assert.NotNull(scope.DbContext.Database.CurrentTransaction);

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithSaveChangesAsync_ShouldStartTransaction(Overload overload)
		{
			await this.Execute(overload, this.Provider, async (scope, ct) =>
			{
				Assert.Null(scope.DbContext.Database.CurrentTransaction);

				var entityToAdd = new TestEntity() { Id = 1 };
				scope.DbContext.Add(entityToAdd);
				await scope.DbContext.SaveChangesAsync();

				Assert.NotNull(scope.DbContext.Database.CurrentTransaction);

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithCustomQueryDuringTransaction_ShouldContinueTransaction(Overload overload)
		{
			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				Assert.Null(scope.DbContext.Database.CurrentTransaction);

				scope.DbContext.Database.BeginTransaction();

				var transaction = scope.DbContext.Database.CurrentTransaction;
				Assert.NotNull(transaction);

				scope.DbContext.Database.ExecuteSqlRaw("SELECT sqlite_version();");

				Assert.Equal(transaction, scope.DbContext.Database.CurrentTransaction);

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithAsyncCustomQueryDuringTransaction_ShouldContinueTransaction(Overload overload)
		{
			await this.Execute(overload, this.Provider, async (scope, ct) =>
			{
				Assert.Null(scope.DbContext.Database.CurrentTransaction);

				await scope.DbContext.Database.BeginTransactionAsync();

				var transaction = scope.DbContext.Database.CurrentTransaction;
				Assert.NotNull(transaction);

				await scope.DbContext.Database.ExecuteSqlRawAsync("SELECT sqlite_version();");

				Assert.Equal(transaction, scope.DbContext.Database.CurrentTransaction);

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithSaveChangesDuringTransaction_ShouldContinueTransaction(Overload overload)
		{
			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				Assert.Null(scope.DbContext.Database.CurrentTransaction);

				scope.DbContext.Database.BeginTransaction();

				var transaction = scope.DbContext.Database.CurrentTransaction;
				Assert.NotNull(transaction);

				var entityToAdd = new TestEntity() { Id = 1 };
				scope.DbContext.Add(entityToAdd);
				scope.DbContext.SaveChanges();

				Assert.Equal(transaction, scope.DbContext.Database.CurrentTransaction);

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithSaveChangesAsyncDuringTransaction_ShouldContinueTransaction(Overload overload)
		{
			await this.Execute(overload, this.Provider, async (scope, ct) =>
			{
				Assert.Null(scope.DbContext.Database.CurrentTransaction);

				await scope.DbContext.Database.BeginTransactionAsync();

				var transaction = scope.DbContext.Database.CurrentTransaction;
				Assert.NotNull(transaction);

				var entityToAdd = new TestEntity() { Id = 1 };
				scope.DbContext.Add(entityToAdd);
				await scope.DbContext.SaveChangesAsync();

				Assert.Equal(transaction, scope.DbContext.Database.CurrentTransaction);

				return Task.FromResult(true);
			});
		}
	}
}
