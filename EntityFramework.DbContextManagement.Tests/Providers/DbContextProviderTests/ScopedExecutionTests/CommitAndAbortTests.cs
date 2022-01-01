using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
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
		public async Task WithAbortCall_ShouldRollBack(Overload overload)
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
		public async Task WithPrematureAbortCall_ShouldPerformOperationsAndThenRollBack(Overload overload)
		{
			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				// Doing this early is just fine
				// It will only take effect once the current execution scope ends
				scope.Abort();

				var entityToInsert = new TestEntity() { Id = 1 };
				scope.DbContext.Add(entityToInsert);
				scope.DbContext.SaveChanges();

				var uncommittedEntityCount = ((TestDbContext)scope.DbContext).TestEntities.Count();
				Assert.Equal(1, uncommittedEntityCount);

				return Task.FromResult(true);
			});

			using var dbContext = this.TestDbContextFactory.CreateDbContext();

			var entityCount = dbContext.TestEntities.Count();

			Assert.Equal(0, entityCount);
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithUncaughtException_ShouldRollBack(Overload overload)
		{
			try
			{
				await this.Execute<bool>(overload, this.Provider, (scope, ct) =>
				{
					var entityToInsert = new TestEntity() { Id = 1 };
					scope.DbContext.Add(entityToInsert);
					scope.DbContext.SaveChanges();

					throw new Exception("This should cause the scoped execution to abort and roll back.");
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

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithPrematureAbortCallInNestedScope_ShouldPerformOperationsAndThenRollBack(Overload overload)
		{
			await this.Execute(overload, this.Provider, async (scope, ct) =>
			{
				await this.Execute(overload, this.Provider, (scope, ct) =>
				{
					// Doing this early is just fine
					// It will only take effect once the current execution scope ends
					scope.Abort();

					var entityToInsert = new TestEntity() { Id = 1 };
					scope.DbContext.Add(entityToInsert);
					scope.DbContext.SaveChanges();

					var uncommittedEntityCount = ((TestDbContext)scope.DbContext).TestEntities.Count();
					Assert.Equal(1, uncommittedEntityCount);

					return Task.FromResult(true);
				}, cancellationToken: ct);

				// We should have rolled back when the inner scope was disposed
				{
					using var dbContext = this.TestDbContextFactory.CreateDbContext();

					dbContext.Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted); // By reading even uncommitted changes, we can confirm that the change was rolled back

					var entityCount = dbContext.TestEntities.Count();

					Assert.Equal(0, entityCount);
				}

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithPrematureAbortCallInNestedScope_ShouldThrowOnDbContextUseInOuterScope(Overload overload)
		{
			await this.Execute(overload, this.Provider, async (scope, ct) =>
			{
				await this.Execute(overload, this.Provider, (scope, ct) =>
				{
					// Doing this early is just fine
					// It will only take effect once the current execution scope ends
					scope.Abort();

					var entityToInsert = new TestEntity() { Id = 1 };
					scope.DbContext.Add(entityToInsert);
					scope.DbContext.SaveChanges();

					return Task.FromResult(true);
				}, cancellationToken: ct);

				// We should be unable to use the DbContext any longer
				Assert.Throws<TransactionAbortedException>(() => ((TestDbContext)scope.DbContext).TestEntities.Count());
				Assert.Throws<TransactionAbortedException>(() => scope.DbContext.SaveChanges());

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithUncaughtExceptionInNestedScope_ShouldPerformOperationsAndThenRollBack(Overload overload)
		{
			await this.Execute(overload, this.Provider, async (scope, ct) =>
			{
				try
				{
					await this.Execute<bool>(overload, this.Provider, (scope, ct) =>
					{
						// Doing this early is just fine
						// It will only take effect once the current execution scope ends
						scope.Abort();

						var entityToInsert = new TestEntity() { Id = 1 };
						scope.DbContext.Add(entityToInsert);
						scope.DbContext.SaveChanges();

						var uncommittedEntityCount = ((TestDbContext)scope.DbContext).TestEntities.Count();
						Assert.Equal(1, uncommittedEntityCount);

						throw new Exception("This should cause the scoped execution to abort and roll back.");
					}, cancellationToken: ct);
				}
				catch
				{
					// We only threw to test that the scoped execution would abort
				}

				// We should have rolled back when the inner scope was disposed
				{
					using var dbContext = this.TestDbContextFactory.CreateDbContext();

					dbContext.Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted); // By reading even uncommitted changes, we can confirm that the change was rolled back

					var entityCount = dbContext.TestEntities.Count();

					Assert.Equal(0, entityCount);
				}

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithUncaughtExceptionInNestedScope_ShouldThrowOnDbContextUseInOuterScope(Overload overload)
		{
			await this.Execute(overload, this.Provider, async (scope, ct) =>
			{
				try
				{
					await this.Execute<bool>(overload, this.Provider, (scope, ct) =>
					{
						// Doing this early is just fine
						// It will only take effect once the current execution scope ends
						scope.Abort();

						var entityToInsert = new TestEntity() { Id = 1 };
						scope.DbContext.Add(entityToInsert);
						scope.DbContext.SaveChanges();

						throw new Exception("This should cause the scoped execution to abort and roll back.");
					}, cancellationToken: ct);
				}
				catch
				{
					// We only threw to test that the scoped execution would abort
				}

				// We should be unable to use the DbContext any longer
				Assert.Throws<TransactionAbortedException>(() => ((TestDbContext)scope.DbContext).TestEntities.Count());
				Assert.Throws<TransactionAbortedException>(() => scope.DbContext.SaveChanges());

				return Task.FromResult(true);
			});
		}
	}
}
