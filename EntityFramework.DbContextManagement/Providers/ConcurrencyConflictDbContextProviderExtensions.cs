using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// Provides extension methods to register a <see cref="ConcurrencyConflictDbContextProvider{TContext, TDbContext}"/>.
	/// </summary>
	public static class ConcurrencyConflictDbContextProviderExtensions
	{
		/// <summary>
		/// <para>
		/// Registers an <see cref="IDbContextProvider{TContext}"/> implementation that wraps the original one,
		/// throwing a <see cref="DbUpdateConcurrencyException"/> on the first attempt, at the end of the executed task.
		/// </para>
		/// <para>
		/// By executing all of the work before throwing, the most mistakes are detected (such as an auto-increment ID already being assigned when the next attempt begins).
		/// </para>
		/// </summary>
		/// <param name="afterCommit">If true, any ongoing transaction is committed before the exception occurs, simulating an exception on commit where the commit has actually succeeded.</param>
		public static IServiceCollection AddConcurrencyConflictDbContextProvider<TDbContext>(this IServiceCollection services, bool afterCommit = false)
			where TDbContext : DbContext
		{
			return AddConcurrencyConflictDbContextProvider<TDbContext, TDbContext>(services, afterCommit);
		}

		/// <summary>
		/// <para>
		/// Registers an <see cref="IDbContextProvider{TContext}"/> implementation that wraps the original one,
		/// throwing a <see cref="DbUpdateConcurrencyException"/> on the first attempt, at the end of the executed task.
		/// </para>
		/// <para>
		/// By executing all of the work before throwing, the most mistakes are detected (such as an auto-increment ID already being assigned when the next attempt begins).
		/// </para>
		/// </summary>
		/// <param name="afterCommit">If true, any ongoing transaction is committed before the exception occurs, simulating an exception on commit where the commit has actually succeeded.</param>
		public static IServiceCollection AddConcurrencyConflictDbContextProvider<TDbContextRepresentation, TDbContext>(this IServiceCollection services, bool afterCommit = false)
			where TDbContext : DbContext
		{
			var options = new DbContextScopeExtensions.Options<TDbContext>(services);

			var previousRegistration = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IDbContextProvider<TDbContextRepresentation>)) ??
				throw new InvalidOperationException($"No {nameof(IDbContextProvider<TDbContextRepresentation>)} was registered that we could wrap.");
			var implementationFactory = previousRegistration.ImplementationFactory ??
				(previousRegistration.ImplementationInstance is not null
					? _ => previousRegistration.ImplementationInstance
					: serviceProvider => ActivatorUtilities.CreateInstance(serviceProvider, previousRegistration.ImplementationType!));

			services.AddSingleton<IDbContextProvider<TDbContextRepresentation>>(CreateInstance);
			services.AddSingleton<IDbContextProvider<TDbContext>>(CreateInstance);
			return services;

			ConcurrencyConflictDbContextProvider<TDbContextRepresentation, TDbContext> CreateInstance(IServiceProvider serviceProvider)
			{
				var wrappedProvider = (IDbContextProvider<TDbContextRepresentation>)implementationFactory(serviceProvider) ??
					throw new Exception($"Implementation factory produced a null {nameof(IDbContextProvider<TDbContextRepresentation>)} instance.");
				var instance = new ConcurrencyConflictDbContextProvider<TDbContextRepresentation, TDbContext>(wrappedProvider, afterCommit);
				return instance;
			}
		}
	}
}
