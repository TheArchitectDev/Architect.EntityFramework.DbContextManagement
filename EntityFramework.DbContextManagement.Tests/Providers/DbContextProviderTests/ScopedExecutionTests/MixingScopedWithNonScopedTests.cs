using System;
using System.Threading.Tasks;
using Architect.AmbientContexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Architect.EntityFramework.DbContextManagement.Tests.Providers.DbContextProviderTests.ScopedExecutionTests
{
	public class MixingScopedWithNonScopedTests : ScopedExecutionTestBase
	{
		/// <summary>
		/// This causes the scoped execution to lack the control it needs to do its job, since that control resides with the (regular) outer scope.
		/// </summary>
		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithinRegularScope_ShouldThrow(Overload overload)
		{
			using var scope = this.Provider.CreateDbContextScope();

			await Assert.ThrowsAsync<InvalidOperationException>(() => this.Execute(overload, this.Provider, (scope, ct) => Task.FromResult(true)));
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloadsWithScopeOption))]
		public async Task WithinRegularScopeWithForceCreateNew_ShouldBeUnrelated(Overload overload)
		{
			using var scope = this.Provider.CreateDbContextScope();

			var outerDbContext = scope.DbContext;
			DbContext innerDbContext = null;

			await this.Execute(overload, this.Provider, scopeOption: AmbientScopeOption.ForceCreateNew, task: (scope, ct) =>
			{
				innerDbContext = scope.DbContext;
				return Task.FromResult(true);
			});

			Assert.NotNull(outerDbContext);
			Assert.NotNull(innerDbContext);
			Assert.NotEqual(outerDbContext, innerDbContext);
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithRegularScopeCreatedInside_ShouldUseThatScope(Overload overload)
		{
			DbContext outerDbContext = null;
			DbContext innerDbContext = null;

			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				outerDbContext = scope.DbContext;
				
				var innerScope = this.Provider.CreateDbContextScope();

				innerDbContext = innerScope.DbContext;

				return Task.FromResult(true);
			});

			Assert.NotNull(outerDbContext);
			Assert.NotNull(innerDbContext);
			Assert.Equal(outerDbContext, innerDbContext);
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithRegularScopeCreatedInsideWithForceCreateNew_ShouldBeUnrelated(Overload overload)
		{
			DbContext outerDbContext = null;
			DbContext innerDbContext = null;

			await this.Execute(overload, this.Provider, (scope, ct) =>
			{
				outerDbContext = scope.DbContext;

				var innerScope = this.Provider.CreateDbContextScope(AmbientScopeOption.ForceCreateNew);

				innerDbContext = innerScope.DbContext;

				return Task.FromResult(true);
			});

			Assert.NotNull(outerDbContext);
			Assert.NotNull(innerDbContext);
			Assert.NotEqual(outerDbContext, innerDbContext);
		}
	}
}
