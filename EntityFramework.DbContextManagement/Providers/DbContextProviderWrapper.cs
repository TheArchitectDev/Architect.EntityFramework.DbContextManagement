using System;
using Architect.AmbientContexts;
using Microsoft.EntityFrameworkCore;

namespace Architect.EntityFramework.DbContextManagement.Providers
{
	/// <summary>
	/// <para>
	/// Creates an <see cref="IDbContextProvider{TContext}"/> of <typeparamref name="TContext"/>. <typeparamref name="TContext"/> does not need to be a <see cref="DbContext"/> type.
	/// </para>
	/// <para>
	/// The implementation simply forwards everything to a wrapped <see cref="IInternalDbContextProvider{TDbContext}"/> of <typeparamref name="TDbContext"/>.
	/// <typeparamref name="TDbContext"/> <strong>is</strong> a <see cref="DbContext"/> type.
	/// </para>
	/// <para>
	/// This makes it possible to let <typeparamref name="TContext"/> be an abstraction that represents <typeparamref name="TDbContext"/>, so that <typeparamref name="TDbContext"/> can remain internal.
	/// </para>
	/// </summary>
	internal sealed class DbContextProviderWrapper<TContext, TDbContext> : DbContextProvider<TContext, TDbContext>
		where TDbContext : DbContext
	{
		private IInternalDbContextProvider<TDbContext> Provider { get; }

		public override DbContextScopeOptions Options => this.Provider.Options;

		public DbContextProviderWrapper(IInternalDbContextProvider<TDbContext> provider)
		{
			this.Provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}

		public override DbContextScope CreateDbContextScope(AmbientScopeOption? scopeOption = null)
		{
			return this.Provider.CreateDbContextScope(scopeOption);
		}
	}
}
