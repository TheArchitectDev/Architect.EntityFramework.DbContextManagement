using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Architect.EntityFramework.DbContextManagement.Tests.Providers.DbContextProviderTests.ScopedExecutionTests
{
	public class CommitAndAbortTests : ScopedExecutionTestBase
	{
		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task Regularly_ShouldCommit(Overload overload)
		{
			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				var entityToInsert = new TestEntity() { Id = 1 };
				scope.DbContext.Add(entityToInsert);
				scope.DbContext.SaveChanges();

				return Task.FromResult(true);
			});

			using var dbContext = this.TestDbContextFactory.CreateDbContext();

			var entityCount = dbContext.TestEntities.Count();

			Assert.Equal(1, entityCount);
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithAbortCall_ShouldAbort(Overload overload)
		{
			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				var entityToInsert = new TestEntity() { Id = 1 };
				scope.DbContext.Add(entityToInsert);
				scope.DbContext.SaveChanges();

				scope.Abort();

				return Task.FromResult(true);
			});

			using var dbContext = this.TestDbContextFactory.CreateDbContext();

			var entityCount = dbContext.TestEntities.Count();

			Assert.Equal(0, entityCount);
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithPrematureAbortCall_ShouldPerformOperationsAndThenAbort(Overload overload)
		{
			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				// Doing this early is just fine
				// It will only take effect once the scoped execution ends
				scope.Abort();

				var entityToInsert = new TestEntity() { Id = 1 };
				scope.DbContext.Add(entityToInsert);
				scope.DbContext.SaveChanges();

				return Task.FromResult(true);
			});

			using var dbContext = this.TestDbContextFactory.CreateDbContext();

			var entityCount = dbContext.TestEntities.Count();

			Assert.Equal(0, entityCount);
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithUncaughtException_ShouldAbort(Overload overload)
		{
			try
			{
				await this.Execute<bool>(overload, this.Provider, (scope, ct) =>
				{
					var entityToInsert = new TestEntity() { Id = 1 };
					scope.DbContext.Add(entityToInsert);
					scope.DbContext.SaveChanges();

					throw new Exception("This should cause the scoped execution to abort.");
				});
			}
			catch
			{
				// We only threw to test that the scoped execution would abort
			}

			using var dbContext = this.TestDbContextFactory.CreateDbContext();

			var entityCount = dbContext.TestEntities.Count();

			Assert.Equal(0, entityCount);
		}
	}
}
