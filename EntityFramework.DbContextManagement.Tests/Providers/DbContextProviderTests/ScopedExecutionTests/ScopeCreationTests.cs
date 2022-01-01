using System;
using System.Threading.Tasks;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement.DbContextScopes;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Architect.EntityFramework.DbContextManagement.Tests.Providers.DbContextProviderTests.ScopedExecutionTests
{
	public class ScopeCreationTests : ScopedExecutionTestBase
	{
		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task Regularly_ShouldCreateScope(Overload overload)
		{
			var accessor = new AmbientDbContextAccessor<TestDbContext>();

			await this.Execute(overload, this.Provider, async (scope, ct) =>
			{
				Assert.NotNull(DbContextScope<TestDbContext>.CurrentOrDefault);
				Assert.Equal(DbContextScope<TestDbContext>.CurrentDbContext, accessor.CurrentDbContext);

				if (this.IsAsync(overload))
					await Task.Delay(TimeSpan.FromTicks(1), ct);

				Assert.NotNull(DbContextScope<TestDbContext>.CurrentOrDefault);
				Assert.Equal(DbContextScope<TestDbContext>.CurrentDbContext, accessor.CurrentDbContext);

				return true;
			});
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task Regularly_ShouldDisposeScope(Overload overload)
		{
			var accessor = new AmbientDbContextAccessor<TestDbContext>();

			Assert.Null(DbContextScope<TestDbContext>.CurrentOrDefault);
			Assert.Throws<InvalidOperationException>(() => accessor.CurrentDbContext);

			await this.Execute(overload, this.Provider, async (scope, ct) =>
			{
				if (this.IsAsync(overload))
					await Task.Delay(TimeSpan.FromTicks(1), ct);

				return true;
			});

			Assert.Null(DbContextScope<TestDbContext>.CurrentOrDefault);
			Assert.Throws<InvalidOperationException>(() => accessor.CurrentDbContext);
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloadsWithScopeOption))]
		public async Task NestedWithNoNesting_ShouldThrow(Overload overload)
		{
			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				this.Execute(overload, this.Provider, task: (scope, ct) =>
					this.Execute(overload, this.Provider, scopeOption: AmbientScopeOption.NoNesting, task: (scope, ct) => Task.FromResult(true), cancellationToken: ct)));
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloadsWithScopeOption))]
		public async Task NestedWithForceCreateNew_ShouldHaveDifferentUnitOfWorkAndDbContext(Overload overload)
		{
			UnitOfWork outerUnitOfWork = null;
			UnitOfWork innerUnitOfWork = null;
			DbContext outerDbContext = null;
			DbContext innerDbContext = null;

			await this.Execute(overload, this.Provider, task: (scope, ct) =>
			{
				var outerScope = DbContextScope<TestDbContext>.Current;
				outerUnitOfWork = outerScope.UnitOfWork;
				outerDbContext = outerScope.DbContext;

				return this.Execute(overload, this.Provider, scopeOption: AmbientScopeOption.ForceCreateNew, task: (scope, ct) =>
				{
					var innerScope = DbContextScope<TestDbContext>.Current;
					innerUnitOfWork = innerScope.UnitOfWork;
					innerDbContext = innerScope.DbContext;
					return Task.FromResult(true);
				}, cancellationToken: ct);
			});

			Assert.NotNull(outerUnitOfWork);
			Assert.NotNull(innerUnitOfWork);
			Assert.NotNull(outerDbContext);
			Assert.NotNull(innerDbContext);

			Assert.NotEqual(outerUnitOfWork, innerUnitOfWork);
			Assert.NotEqual(outerDbContext, innerDbContext);
		}

		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task NestedWithJoinExisting_ShouldHaveSameUnitOfWorkAndDbContext(Overload overload)
		{
			UnitOfWork outerUnitOfWork = null;
			UnitOfWork innerUnitOfWork = null;
			DbContext outerDbContext = null;
			DbContext innerDbContext = null;

			await this.Execute(overload, this.Provider, task: (scope, ct) =>
			{
				var outerScope = DbContextScope<TestDbContext>.Current;
				outerUnitOfWork = outerScope.UnitOfWork;
				outerDbContext = outerScope.DbContext;

				return this.Execute(overload, this.Provider, scopeOption: AmbientScopeOption.JoinExisting, task: (scope, ct) =>
				{
					var innerScope = DbContextScope<TestDbContext>.Current;
					innerUnitOfWork = innerScope.UnitOfWork;
					innerDbContext = innerScope.DbContext;
					return Task.FromResult(true);
				}, cancellationToken: ct);
			});

			Assert.NotNull(outerUnitOfWork);
			Assert.NotNull(innerUnitOfWork);
			Assert.NotNull(outerDbContext);
			Assert.NotNull(innerDbContext);

			Assert.Equal(outerUnitOfWork, innerUnitOfWork);
			Assert.Equal(outerDbContext, innerDbContext);
		}
	}
}
