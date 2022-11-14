using Xunit;

namespace Architect.EntityFramework.DbContextManagement.Tests.Providers;

public class MockDbContextProviderTests
{
	private MockDbContextProvider<TestDbContext> Instance { get; } = new MockDbContextProvider<TestDbContext>();

	[Fact]
	public async Task ExecuteInDbContextScopeAsync_NestedInAnother_ShouldHaveExpectedEffect()
	{
		var invocationCount = 0;

		await this.Instance.ExecuteInDbContextScopeAsync(_ =>
		{
			invocationCount++;

			return this.Instance.ExecuteInDbContextScopeAsync(_ =>
			{
				invocationCount++;

				return Task.CompletedTask;
			});
		});

		Assert.Equal(2, invocationCount);
	}

	[Fact]
	public async Task ExecuteInDbContextScopeAsync_NestedInRegularScope_ShouldThrow()
	{
		await using var dbContextScope = this.Instance.CreateDbContextScope();

		await Assert.ThrowsAsync<InvalidOperationException>(() => this.Instance.ExecuteInDbContextScopeAsync(_ =>
		{
			 using var dbContextScope = this.Instance.CreateDbContextScope();
			return Task.CompletedTask;
		}));
	}

	[Fact]
	public async Task CreateDbContextScope_NestedInAnother_ShouldHaveExpectedEffect()
	{
		var invocationCount = 0;

		await using var dbContextScope1 = this.Instance.CreateDbContextScope();
		invocationCount++;

		await using var dbContextScope2 = this.Instance.CreateDbContextScope();
		invocationCount++;

		Assert.Equal(2, invocationCount);
	}

	[Fact]
	public async Task CreateDbContextScope_NestedInScopedExecution_ShouldHaveExpectedEffect()
	{
		var invocationCount = 0;

		await this.Instance.ExecuteInDbContextScopeAsync(async _ =>
		{
			invocationCount++;

			await using var dbContextScope = this.Instance.CreateDbContextScope();
			invocationCount++;
		});

		Assert.Equal(2, invocationCount);
	}
}
