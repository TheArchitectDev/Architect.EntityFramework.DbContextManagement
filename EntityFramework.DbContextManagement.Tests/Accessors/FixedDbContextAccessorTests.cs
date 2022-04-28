using Xunit;

namespace Architect.EntityFramework.DbContextManagement.Tests.Accessors
{
	public class FixedDbContextAccessorTests
	{
		private TestDbContext DbContext { get; set; }

		[Theory]
		[InlineData(CreationType.Instance)]
		[InlineData(CreationType.Factory)]
		public void HasDbContext_Regularly_ShouldReturnTrue(CreationType creationType)
		{
			var accessor = this.Create(creationType);

			var result = accessor.HasDbContext;

			Assert.True(result);
		}

		[Theory]
		[InlineData(CreationType.Instance)]
		[InlineData(CreationType.Factory)]
		public void CurrentDbContext_Regularly_ShouldReturnExpectedResult(CreationType creationType)
		{
			var accessor = this.Create(creationType);

			var result = accessor.CurrentDbContext;

			Assert.NotNull(result);
			Assert.Equal(this.DbContext, result);
		}

		/// <summary>
		/// Creates a <see cref="FixedDbContextAccessor{TDbContext}"/>, its <see cref="Microsoft.EntityFrameworkCore.DbContext"/> accessible through the <see cref="DbContext"/> property.
		/// </summary>
		private FixedDbContextAccessor<TestDbContext> Create(CreationType creationType)
		{
			return creationType switch
			{
				CreationType.Instance => FixedDbContextAccessor.Create(dbContext: this.CreateAndRememberDbContext()),
				CreationType.Factory => FixedDbContextAccessor.Create(this.CreateAndRememberDbContext),
				_ => throw new NotImplementedException(),
			};
		}

		private TestDbContext CreateAndRememberDbContext()
		{
			this.DbContext = TestDbContext.Create();
			return this.DbContext;
		}

		public enum CreationType : byte
		{
			Instance,
			Factory,
		}
	}
}
