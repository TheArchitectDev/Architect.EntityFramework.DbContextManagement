using System;
using System.Linq;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// Provides extension methods to register ambient <see cref="DbContext"/> usage.
	/// </summary>
	public static class DbContextScopeExtensions
	{
		/// <summary>
		/// <para>
		/// Registers an <see cref="IDbContextProvider{TContext}"/> and <see cref="IDbContextAccessor{TDbContext}"/> for the given <see cref="DbContext"/>.
		/// </para>
		/// <para>
		/// This overload allows any <typeparamref name="TDbContextRepresentation"/> to represent <typeparamref name="TDbContext"/> for the <see cref="IDbContextProvider{TContext}"/>.
		/// This way, visibility of <typeparamref name="TDbContext"/> can be restricted to the data access layer,
		/// while the composition root can still control the <see cref="DbContext"/> and transaction boundaries.
		/// </para>
		/// <para>
		/// From the composition root (e.g. an application service), inject an <see cref="IDbContextProvider{TDbContext}"/> to create a scope.
		/// Provide a single <see cref="DbContext"/> by calling
		/// <see cref="IDbContextProvider{TContext}.ExecuteInDbContextScope"/> (recommended) or <see cref="IDbContextProvider{TContext}.CreateDbContextScope"/>.
		/// </para>
		/// <para>
		/// From the data access code (e.g. a repository), inject an <see cref="IDbContextAccessor{TDbContext}"/> to access the current <see cref="DbContext"/>.
		/// </para>
		/// <para>
		/// This way, the composition root controls the <see cref="DbContext"/> and the transaction boundaries.
		/// The data access code simply uses the <see cref="DbContext"/> that the composition root has made available.
		/// </para>
		/// </summary>
		/// <typeparam name="TDbContextRepresentation">A type used to represent <typeparamref name="TDbContext"/> without being able to see it.</typeparam>
		/// <typeparam name="TDbContext">The type of <see cref="DbContext"/> to work with.</typeparam>
		public static IServiceCollection AddDbContextScope<TDbContextRepresentation, TDbContext>(this IServiceCollection services, Action<Options<TDbContext>>? scopeOptions = null)
			where TDbContext : DbContext
		{
			AddDbContextScope(services, scopeOptions);

			var options = new Options<TDbContext>(services);
			scopeOptions?.Invoke(options);

			var implementationFactory = GetFactoryForDbContextProviderWithOptions(options);
			AddDbContextScopeCore(options, CreateInstance);

			return services;

			// Local function that creates a new instance
			IDbContextProvider<TDbContextRepresentation> CreateInstance(IServiceProvider serviceProvider)
			{
				var wrappedInstance = implementationFactory(serviceProvider) ?? throw new Exception($"Implementation factory produced a null {nameof(IDbContextProvider<TDbContext>)} instance.");
				var instance = new DbContextProviderWrapper<TDbContextRepresentation, TDbContext>(wrappedInstance);
				return instance;
			}
		}

		/// <summary>
		/// <para>
		/// Registers an <see cref="IDbContextProvider{TContext}"/> and <see cref="IDbContextAccessor{TDbContext}"/> for the given <typeparamref name="TDbContext"/>.
		/// </para>
		/// <para>
		/// From the composition root (e.g. an application service), inject an <see cref="IDbContextProvider{TDbContext}"/> to create a scope.
		/// Provide a single <see cref="DbContext"/> by calling
		/// <see cref="IDbContextProvider{TContext}.ExecuteInDbContextScope"/> (recommended) or <see cref="IDbContextProvider{TContext}.CreateDbContextScope"/>.
		/// </para>
		/// <para>
		/// From the data access code (e.g. a repository), inject an <see cref="IDbContextAccessor{TDbContext}"/> to access the current <see cref="DbContext"/>.
		/// </para>
		/// <para>
		/// This way, the composition root controls the <see cref="DbContext"/> and the transaction boundaries.
		/// The data access code simply uses the <see cref="DbContext"/> that the composition root has made available.
		/// </para>
		/// </summary>
		/// <typeparam name="TDbContext">The type of <see cref="DbContext"/> to work with.</typeparam>
		public static IServiceCollection AddDbContextScope<TDbContext>(this IServiceCollection services, Action<Options<TDbContext>>? scopeOptions = null)
			where TDbContext : DbContext
		{
			var options = new Options<TDbContext>(services);
			scopeOptions?.Invoke(options);

			var implementationFactory = GetFactoryForDbContextProviderWithOptions(options);
			AddDbContextScopeCore(options, implementationFactory);

			return services;
		}

		private static void AddDbContextScopeCore<TDbContext, TDbContextRepresentation>(
			Options<TDbContext> options, Func<IServiceProvider, IDbContextProvider<TDbContextRepresentation>> providerImplementationFactory)
			where TDbContext : DbContext
		{
			options.Services.AddSingleton<IDbContextAccessor<TDbContext>, AmbientDbContextAccessor<TDbContext>>();
			options.Services.AddSingleton(providerImplementationFactory);
		}

		/// <summary>
		/// Returns a new implementation factory for the <see cref="IDbContextProvider{TContext}"/>.
		/// </summary>
		internal static Func<IServiceProvider, DbContextProvider<TDbContext>> GetFactoryForDbContextProviderWithOptions<TDbContext>(Options<TDbContext> options)
			where TDbContext : DbContext
		{
			if (options.DbContexFactory is null)
			{
				if (!options.Services.Any(descriptor => descriptor.ServiceType == typeof(IDbContextFactory<TDbContext>)))
				{
					throw new ArgumentException($"First call {nameof(EntityFrameworkServiceCollectionExtensions.AddPooledDbContextFactory)}() or {nameof(EntityFrameworkServiceCollectionExtensions.AddDbContextFactory)}(), or use the options to configure the {nameof(options.DbContexFactory)}.");
				}
			}

			var dbContextFactoryFunction = options.DbContexFactory;
			var dbContextProviderOptions = options.DbContextProviderOptions;

			return CreateDbContextProvider;

			// Local function that returns a new provider instance
			DbContextProvider<TDbContext> CreateDbContextProvider(IServiceProvider serviceProvider)
			{
				var factory = dbContextFactoryFunction is null
					? serviceProvider.GetRequiredService<IDbContextFactory<TDbContext>>()
					: new DbContextFactory<TDbContext>(serviceProvider, dbContextFactoryFunction);
				var instance = new DbContextProvider<TDbContext>(factory, dbContextProviderOptions);
				return instance;
			}
		}

		public sealed class Options<TDbContext>
			where TDbContext : DbContext
		{
			internal IServiceCollection Services { get; }
			internal Func<IServiceProvider, TDbContext>? DbContexFactory { get; set; }
			internal DbContextScopeOptionsBuilder OptionsBuilder { get; } = new DbContextScopeOptionsBuilder();
			internal DbContextScopeOptions DbContextProviderOptions => this.OptionsBuilder.Build();

			public Options(IServiceCollection services)
			{
				this.Services = services ?? throw new ArgumentNullException(nameof(services));
			}
		}

		/// <summary>
		/// <para>
		/// Registers a custom <see cref="DbContext"/> factory to use for scoped <see cref="DbContext"/> access.
		/// </para>
		/// <para>
		/// Each invocation must produce a new instance (or effectively new if context pooling is used). Instances are disposed once they are no longer needed.
		/// </para>
		/// </summary>
		public static Options<TDbContext> DbContextFactory<TDbContext>(this Options<TDbContext> options, Func<TDbContext> dbContextFactory)
			where TDbContext : DbContext
		{
			if (options is null) throw new ArgumentNullException(nameof(options));
			if (dbContextFactory is null) throw new ArgumentNullException(nameof(dbContextFactory));

			options.DbContexFactory = _ => dbContextFactory();
			return options;
		}

		public static Options<TDbContext> ExecutionStrategyOptions<TDbContext>(this Options<TDbContext> options, ExecutionStrategyOptions executionStrategyOptions)
			where TDbContext : DbContext
		{
			options.OptionsBuilder.ExecutionStrategyOptions = executionStrategyOptions;
			return options;
		}

		public static Options<TDbContext> DefaultScopeOption<TDbContext>(this Options<TDbContext> options, AmbientScopeOption defaultScopeOption)
			where TDbContext : DbContext
		{
			if (!Enum.IsDefined(typeof(AmbientScopeOption), defaultScopeOption))
				throw new ArgumentException($"Undefined {nameof(AmbientScopeOption)}: {defaultScopeOption}.");

			options.OptionsBuilder.DefaultScopeOption = defaultScopeOption;
			return options;
		}
	}
}
