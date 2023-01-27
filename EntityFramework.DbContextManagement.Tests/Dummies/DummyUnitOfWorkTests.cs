using Architect.EntityFramework.DbContextManagement.Dummies;
using Xunit;

namespace Architect.EntityFramework.DbContextManagement.Tests.Dummies
{
	public sealed class DummyUnitOfWorkTests
	{
		[Fact]
		public void TryRollBackTransaction_WhenAborted_ShouldSucceed()
		{
			var instance = new DummyUnitOfWork();

			instance.TryRollBackTransactionAndInvalidate();
			
			Assert.False(instance.TryRollBackTransaction());
		}
	}
}
