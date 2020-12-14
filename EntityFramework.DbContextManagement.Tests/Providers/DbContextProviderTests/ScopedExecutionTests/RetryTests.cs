using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Architect.EntityFramework.DbContextManagement.Tests.Providers.DbContextProviderTests.ScopedExecutionTests
{
	public class RetryTests : ScopedExecutionTestBase
	{
		public RetryTests()
		{
			this.OptionsBuilder.ExecutionStrategyOptions |= ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure;
		}

		private void ThrowExceptionThatShouldTriggerRetry()
		{
			throw new DbUpdateConcurrencyException("Simulated exception to trigger retry.");
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithEndlessRetryableExceptions_ShouldThrowOnceRetryLimitExceeded(Overload overload)
		{
			const int expectedAttemptCount = 3;
			var attemptCount = 0;

			await Assert.ThrowsAsync<RetryLimitExceededException>(() => this.Execute(overload, this.Provider, (scope, ct) =>
			{
				if (attemptCount > 20) throw new Exception("Short-circuited to avoid endless loop.");

				attemptCount++;
				this.ThrowExceptionThatShouldTriggerRetry();
				return Task.FromResult(true);
			}));

			Assert.Equal(expectedAttemptCount, attemptCount);
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithExceptionOnFirstAttempt_ShouldSucceed(Overload overload)
		{
			var attemptCount = 0;

			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				if (attemptCount++ == 0) this.ThrowExceptionThatShouldTriggerRetry(); // Only once

				return Task.FromResult(true);
			});

			Assert.Equal(2, attemptCount);
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithExceptionOnFirstAttempt_ShouldCommitOnSuccess(Overload overload)
		{
			var attemptCount = 0;

			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				var entityToInsert = new TestEntity() { Id = 1 + attemptCount }; // Unique IDs
				scope.DbContext.Add(entityToInsert);
				scope.DbContext.SaveChanges();

				if (attemptCount++ == 0) this.ThrowExceptionThatShouldTriggerRetry(); // Only once

				return Task.FromResult(true);
			});

			using var dbContext = this.TestDbContextFactory.CreateDbContext();

			var entityCount = dbContext.TestEntities.Count();

			Assert.Equal(1, entityCount);
			Assert.Equal(2, dbContext.TestEntities.Single().Id); // The second entity was saved and committed
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithExceptionOnFirstAttemptAndWithAbort_ShouldAbort(Overload overload)
		{
			var attemptCount = 0;

			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				var entityToInsert = new TestEntity() { Id = 1 + attemptCount }; // Unique IDs
				scope.DbContext.Add(entityToInsert);
				scope.DbContext.SaveChanges();

				scope.Abort();

				if (attemptCount++ == 0) this.ThrowExceptionThatShouldTriggerRetry(); // Only once

				return Task.FromResult(true);
			});

			using var dbContext = this.TestDbContextFactory.CreateDbContext();

			var entityCount = dbContext.TestEntities.Count();

			Assert.Equal(0, entityCount);
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithExceptionOnFirstAttempt_ShouldRollBack(Overload overload)
		{
			var attemptCount = 0;

			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				// Adding the saved entity on retry would cause a duplicate key exception if the transaction had been committed
				var entityToInsert = new TestEntity() { Id = 1 };
				scope.DbContext.Add(entityToInsert);
				scope.DbContext.SaveChanges();

				if (attemptCount++ == 0) this.ThrowExceptionThatShouldTriggerRetry(); // Only once

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithExceptionOnFirstAttempt_ShouldUndoAbort(Overload overload)
		{
			var attemptCount = 0;

			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				var entityToInsert = new TestEntity() { Id = 1 };
				scope.DbContext.Add(entityToInsert);
				scope.DbContext.SaveChanges();
				
				// #TODO: Check if summaries describe Abort() functionality correctly (i.e. only acts once disposal is reached)
				// The exceptions that cause a retry may cause the current scope to abort
				// If the abort were carried across retries, attempts to use the DbContext during the retries would fail
				scope.Abort();

				if (attemptCount++ == 0) this.ThrowExceptionThatShouldTriggerRetry(); // Only once

				return Task.FromResult(true);
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithExceptionOnFirstAttempt_ShouldGetFreshChangeTracker(Overload overload)
		{
			var attemptCount = 0;

			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				// Add and forget the entity
				var entityToInsert = new TestEntity() { Id = 1 };
				scope.DbContext.Add(entityToInsert);
				scope.DbContext.SaveChanges();
				scope.DbContext.Entry(entityToInsert).State = EntityState.Detached;

				// Load the entity
				// Without a fresh ChangeTracker, the next iteration would fail when trying to add an entity with Id=1 (because the loaded one would still be in the change tracker)
				var loadedEntity = ((TestDbContext)scope.DbContext).TestEntities.Single();

				// Confirm that adding a second time would throw, a fact that our test relies on
				Assert.ThrowsAny<Exception>(() => ((TestDbContext)scope.DbContext).TestEntities.Add(entityToInsert));

				if (attemptCount++ == 0) this.ThrowExceptionThatShouldTriggerRetry(); // Only once

				return Task.FromResult(true);
			});
		}
	}
}
