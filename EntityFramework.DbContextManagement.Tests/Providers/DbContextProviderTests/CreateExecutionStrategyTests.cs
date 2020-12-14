using System;
using System.Reflection;
using Architect.EntityFramework.DbContextManagement.ExecutionStrategies;
using Xunit;

namespace Architect.EntityFramework.DbContextManagement.Tests.Providers.DbContextProviderTests
{
	public class CreateExecutionStrategyTests : DbContextProviderTestBase
	{
		[Fact]
		public void CreateExecutionStrategy_WithoutRetryOnOptimisticConcurrencyFailure_ShouldReturnExpectedResult()
		{
			this.OptionsBuilder.ExecutionStrategyOptions = ExecutionStrategyOptions.None;

			var dbContext = TestDbContext.Create();

			var result = this.Provider.CreateExecutionStrategy(dbContext);

			Assert.IsType(dbContext.Database.CreateExecutionStrategy().GetType(), result);
		}

		[Fact]
		public void CreateExecutionStrategy_WithRetryOnOptimisticConcurrencyFailure_ShouldReturnExpectedResult()
		{
			this.OptionsBuilder.ExecutionStrategyOptions = ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure;

			var dbContext = TestDbContext.Create();

			var result = this.Provider.CreateExecutionStrategy(dbContext);

			Assert.IsType<RetryOnOptimisticConcurrencyFailureExecutionStrategy>(result);

			var wrappedStrategyProperty = typeof(RetryOnOptimisticConcurrencyFailureExecutionStrategy).GetProperty("WrappedStrategy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
				throw new Exception("The WrappedStrategy property could not be found.");

			var wrappedStrategy = wrappedStrategyProperty.GetValue(result);

			Assert.IsType(dbContext.Database.CreateExecutionStrategy().GetType(), wrappedStrategy);
		}
	}
}
