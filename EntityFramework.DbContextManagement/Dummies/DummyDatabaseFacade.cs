using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Architect.EntityFramework.DbContextManagement.Dummies
{
	internal sealed class DummyDatabaseFacade : DatabaseFacade
	{
		public override IDbContextTransaction? CurrentTransaction => null;

		public DummyDatabaseFacade(DbContext context)
			: base(context)
		{
		}
	}
}
