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

			var previousRegistration = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IDbContextProvider<TDbContextRepresentation>));
			if (previousRegistration is null) throw new InvalidOperationException($"No {nameof(IDbContextProvider<TDbContextRepresentation>)} was registered that we could wrap.");
			var implementationFactory = previousRegistration.ImplementationFactory ??
				(previousRegistration.ImplementationInstance != null
					? (Func<IServiceProvider, object>)(_ => previousRegistration.ImplementationInstance)
					: (Func<IServiceProvider, object>)(serviceProvider => ActivatorUtilities.CreateInstance(serviceProvider, previousRegistration.ImplementationType!)));

			services.AddTransient<IDbContextProvider<TDbContextRepresentation>>(CreateInstance); // Transient because it tracks the last seen unit of work
			services.AddTransient<IDbContextProvider<TDbContext>>(CreateInstance); // Transient because it tracks the last seen unit of work
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
