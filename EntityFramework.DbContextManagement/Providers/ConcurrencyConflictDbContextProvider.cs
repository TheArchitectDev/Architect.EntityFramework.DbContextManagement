using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// An <see cref="IDbContextProvider{TContext}"/> implementation that wraps the original one, throwing a <see cref="DbUpdateConcurrencyException"/> on the first attempt, at the end of the executed task.
	/// </para>
	/// <para>
	/// By executing all of the work before throwing, the most mistakes are detected (such as an auto-increment ID already being assigned when the next attempt begins).
	/// </para>
	/// </summary>
	internal sealed class ConcurrencyConflictDbContextProvider<TDbContext> : IDbContextProvider<TDbContext>
		where TDbContext : DbContext
	{
		private object? LastSeenUnitOfWork { get; set; }

		public DbContextScopeOptions Options => this.WrappedProvider.Options;

		private IDbContextProvider<TDbContext> WrappedProvider { get; }

		internal ConcurrencyConflictDbContextProvider(IInternalDbContextProvider<TDbContext> internalDbContextProvider)
		{
			this.WrappedProvider = internalDbContextProvider ?? throw new ArgumentNullException(nameof(internalDbContextProvider));
		}

		public ConcurrencyConflictDbContextProvider(IDbContextProvider<TDbContext> wrappedProvider)
		{
			this.WrappedProvider = wrappedProvider ?? throw new ArgumentNullException(nameof(wrappedProvider));
		}

		public DbContextScope CreateDbContextScope(AmbientScopeOption? scopeOption = null)
		{
			return this.WrappedProvider.CreateDbContextScope(scopeOption);
		}

		internal IExecutionStrategy CreateExecutionStrategy(DbContext dbContext)
		{
			return this.WrappedProvider.CreateExecutionStrategy(dbContext);
		}

		TResult IDbContextProvider<TDbContext>.ExecuteInDbContextScope<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, Func<IExecutionScope<TState>, TResult> task)
		{
			return this.WrappedProvider.ExecuteInDbContextScope(scopeOption, state, scope =>
			{
				var shouldThrow = this.ShouldThrow();
				var result = task(scope);
				ThrowConcurrencyException(shouldThrow);
				return result;
			});
		}

		Task<TResult> IDbContextProvider<TDbContext>.ExecuteInDbContextScopeAsync<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			return this.WrappedProvider.ExecuteInDbContextScopeAsync(scopeOption, state, cancellationToken, async (scope, ct) =>
			{
				var shouldThrow = this.ShouldThrow();
				var result = await task(scope, ct);
				ThrowConcurrencyException(shouldThrow);
				return result;
			});
		}

		private bool ShouldThrow()
		{
			// We will only throw (at the end of the operation) if we see a unit of work for the first time
			var unitOfWork = DbContextScope<TDbContext>.Current.UnitOfWork;
			if (unitOfWork == this.LastSeenUnitOfWork) return false;

			this.LastSeenUnitOfWork = unitOfWork;

			return true;
		}

		private void ThrowConcurrencyException(bool shouldThrow)
		{
			if (shouldThrow)
				throw new DbUpdateConcurrencyException("This is a simulated optimistic concurrency exception.");
		}
	}

	// #TODO: Clean up, put somewhere else?
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
		public static IServiceCollection AddConcurrencyConflictDbContextProvider<TDbContext>(this IServiceCollection services)
			where TDbContext : DbContext
		{
			var options = new DbContextScopeExtensions.Options<TDbContext>(services);

			var previousRegistration = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IDbContextProvider<TDbContext>));
			if (previousRegistration is null) throw new InvalidOperationException($"No {nameof(IDbContextProvider<TDbContext>)} was registered that we could wrap.");
			var implementationFactory = previousRegistration.ImplementationFactory ??
				(previousRegistration.ImplementationInstance != null
					? (Func<IServiceProvider, object>)(_ => previousRegistration.ImplementationInstance)
					: (Func<IServiceProvider, object>)(serviceProvider => ActivatorUtilities.CreateInstance(serviceProvider, previousRegistration.ImplementationType!)));

			services.AddTransient(CreateInstance); // Transient because of execution count
			return services;

			IDbContextProvider<TDbContext> CreateInstance(IServiceProvider serviceProvider)
			{
				var wrappedProvider = (IDbContextProvider<TDbContext>)implementationFactory(serviceProvider) ??
					throw new Exception($"Implementation factory produced a null {nameof(IDbContextProvider<TDbContext>)} instance.");
				var instance = new ConcurrencyConflictDbContextProvider<TDbContext>(wrappedProvider);
				return instance;
			}
		}

		// #TODO
		//public static IServiceCollection AddConcurrencyConflictDbContextProvider<TDbContextRepresentation, TDbContext>(IServiceCollection services)
		//	where TDbContext : DbContext
		//{
		//	services.AddSingleton(CreateInstance);
		//	return services;

		//	IDbContextProvider<TDbContext> CreateInstance(IServiceProvider serviceProvider)
		//	{
		//		var wrappedProvider = serviceProvider.GetRequiredService<IInternalDbContextProvider<TDbContext>>();
		//		var instance = new ConcurrencyConflictDbContextProvider<TDbContext>(wrappedProvider);
		//		return instance;
		//	}
		//}
	}
}
