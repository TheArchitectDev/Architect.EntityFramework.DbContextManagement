using System;
using System.Threading.Tasks;
using System.Transactions;
using Xunit;

namespace Architect.EntityFramework.DbContextManagement.Tests.Providers.DbContextProviderTests.ScopedExecutionTests
{
	public class AmbientTransactionTests : ScopedExecutionTestBase
	{
		[Theory]
		[MemberData(nameof(ScopedExecutionTestBase.GetOverloads))]
		public async Task WithAmbientTransaction_ShouldThrow(Overload overload)
		{
			using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

			await Assert.ThrowsAsync<InvalidOperationException>(() => this.Execute(overload, this.Provider, (scope, ct) => Task.FromResult(true)));
		}
	}
}
