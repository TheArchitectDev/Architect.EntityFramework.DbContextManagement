using System;
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
		public async Task WithEndlessRetryableExceptions_ShouldThrowAfterExpectedNumberOfRetries(Overload overload)
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
	}
}
