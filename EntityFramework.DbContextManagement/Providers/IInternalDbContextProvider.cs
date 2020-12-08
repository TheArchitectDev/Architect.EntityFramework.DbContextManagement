using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.Providers
{
	/// <summary>
	/// A type used to represent the <see cref="IDbContextProvider{TContext}"/> a second time.
	/// This allows <see cref="DbContextProviderWrapper{TContext, TDbContext}"/> to inject an <see cref="IInternalDbContextProvider{TDbContext}"/>.
	/// At the same time, it can register itself as the new <see cref="IDbContextProvider{TContext}"/>.
	/// </summary>
	internal interface IInternalDbContextProvider<TDbContext> : IDbContextProvider<TDbContext>
		where TDbContext : DbContext
	{
	}
}
