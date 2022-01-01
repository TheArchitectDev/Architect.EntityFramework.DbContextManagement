using Architect.AmbientContexts;
using Xunit;

namespace Architect.EntityFramework.DbContextManagement.Tests.Accessors
{
	public class AmbientDbContextAccessorTests
	{
		private AmbientDbContextAccessor<TestDbContext> Accessor { get; } = new AmbientDbContextAccessor<TestDbContext>();

		[Fact]
		public void HasDbContext_WithoutDbContext_ShouldReturnExpectedResult()
		{
			var result = this.Accessor.HasDbContext;

			Assert.False(result);
		}

		[Fact]
		public void HasDbContext_WithDbContext_ShouldReturnExpectedResult()
		{
			using var scope = DbContextScope.Create(TestDbContext.Create, AmbientScopeOption.NoNesting);

			var result = this.Accessor.HasDbContext;

			Assert.True(result);
		}

		[Fact]
		public void CurrentDbContext_WithoutDbContext_ShouldThrow()
		{
			Assert.Throws<InvalidOperationException>(() => this.Accessor.CurrentDbContext);
		}

		[Fact]
		public void CurrentDbContext_WithDbContext_ShouldReturnExpectedResult()
		{
			using var scope = DbContextScope.Create(TestDbContext.Create, AmbientScopeOption.NoNesting);

			var result = this.Accessor.CurrentDbContext;

			Assert.NotNull(result);
			Assert.Equal(scope.DbContext, result);
		}
	}
}
