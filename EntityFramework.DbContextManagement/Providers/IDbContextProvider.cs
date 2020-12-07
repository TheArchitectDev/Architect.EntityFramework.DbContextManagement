using Architect.AmbientContexts;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// Provides an ambient <see cref="DbContextScope"/>, code in whose scope can access the <see cref="DbContext"/> through <see cref="IDbContextAccessor{TDbContext}"/>.
	/// </para>
	/// <para>
	/// <typeparamref name="TDbContext"/> may be a <see cref="DbContext"/> type, or a type used to represent one.
	/// Such an indirect representation can be registered with <see cref="DbContextScopeExtensions.AddDbContextScope{TDbContextRepresentation, TDbContext}
	/// (Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{DbContextScopeExtensions.Options{TDbContext}}?)"/>.
	/// </para>
	/// </summary>
	public partial interface IDbContextProvider<TDbContext>
	{
		/// <summary>
		/// <para>
		/// Returns a new <see cref="DbContextScope"/>, setting it as the ambient one until it is disposed.
		/// </para>
		/// </summary>
		/// <param name="scopeOption">Controls the behavior with regards to potential outer scopes.</param>
		DbContextScope CreateDbContextScope(AmbientScopeOption? scopeOption = null);
	}
}
