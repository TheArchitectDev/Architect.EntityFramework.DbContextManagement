using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement.Dummies;
using Architect.EntityFramework.DbContextManagement.ExecutionStrategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// A mock implementation to provide <see cref="DbContextScope"/> instances.
	/// </para>
	/// </summary>
	public class MockDbContextProvider<TDbContext> : IDbContextProvider<TDbContext>
		where TDbContext : DbContext
	{
		public DbContextScopeOptions Options { get; }

		private DbContextFactory<TDbContext> DbContextFactory { get; }

		private MockDbContextProvider(DbContextFactory<TDbContext>? dbContextFactory, DbContextScopeOptions? options)
		{
			this.Options = options ?? DbContextScopeOptions.Default;

			this.DbContextFactory = dbContextFactory ?? new DbContextFactory<TDbContext>(CreateDbContext);

			// Local function that creates a new TDbContext
			static TDbContext CreateDbContext()
			{
				var databaseField = typeof(DbContext).GetField("_database", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
					ThrowHelper.ThrowIncompatibleWithEfVersion<FieldInfo>(new Exception("Field DbContext._database does not exist."));

				var dbContext = (TDbContext)FormatterServices.GetUninitializedObject(typeof(TDbContext));
				
				databaseField.SetValue(dbContext, new DummyDatabaseFacade(dbContext));

				return dbContext;
			}
		}

		/// <summary>
		/// <para>
		/// Constructs a provider based on empty, non-functional <typeparamref name="TDbContext"/> instances.
		/// The constructor of <typeparamref name="TDbContext"/> will be bypassed altogether.
		/// </para>
		/// <para>
		/// This is intended for tests that do not require a realistic implementation.
		/// </para>
		/// </summary>
		public MockDbContextProvider()
			: this(dbContextFactory: null, options: null)
		{
		}

		/// <summary>
		/// <para>
		/// Constructs a provider based on empty, non-functional <typeparamref name="TDbContext"/> instances.
		/// The constructor of <typeparamref name="TDbContext"/> will be bypassed altogether.
		/// </para>
		/// <para>
		/// This is intended for tests that do not require a realistic implementation.
		/// </para>
		/// </summary>
		public MockDbContextProvider(DbContextScopeOptions options)
			: this(dbContextFactory: null, options: options)
		{
		}

		/// <summary>
		/// Constructs a provider based on a <typeparamref name="TDbContext"/> factory.
		/// </summary>
		public MockDbContextProvider(Func<TDbContext> factoryMethod, DbContextScopeOptions? options = null)
		: this(new DbContextFactory<TDbContext>(factoryMethod), options)
		{
		}

		/// <summary>
		/// Constructs a provider based on a fixed <typeparamref name="TDbContext"/>.
		/// </summary>
		public MockDbContextProvider(TDbContext instance, DbContextScopeOptions? options = null)
			: this(new DbContextFactory<TDbContext>(instance), options)
		{
		}

		public virtual DbContextScope CreateDbContextScope(AmbientScopeOption? scopeOption = null)
		{
			var result = DbContextScope.Create(this.DbContextFactory, scopeOption ?? this.Options.DefaultScopeOption);
			return result;
		}

		protected IExecutionStrategy CreateDummyExecutionStrategy(DbContext dbContext)
		{
			// Since we provide mock behavior, do not proceed into the DbContext's own strategy
			return new DummyExecutionStrategy(dbContext);
		}

		IExecutionStrategy IDbContextProvider<TDbContext>.GetExecutionStrategyFromDbContext(DbContext dbContext)
		{
			return this.CreateDummyExecutionStrategy(dbContext);
		}

		TResult IDbContextProvider<TDbContext>.ExecuteInDbContextScope<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, Func<IExecutionScope<TState>, TResult> task)
		{
			return this.ExecuteInDbContextScopeAsync(scopeOption, state, default, ExecuteSynchronously, async: false, getUnitOfWork: _ => new DummyUnitOfWork(), shouldClearChangeTrackerOnRetry: false)
				.AssumeSynchronous();

			// Local function that executes the given task and returns a completed task
			Task<TResult> ExecuteSynchronously(IExecutionScope<TState> executionScope, CancellationToken _)
			{
				var result = task(executionScope);
				return Task.FromResult(result);
			}
		}

		Task<TResult> IDbContextProvider<TDbContext>.ExecuteInDbContextScopeAsync<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			return this.ExecuteInDbContextScopeAsync(scopeOption, state, cancellationToken, task, async: true, getUnitOfWork: _ => new DummyUnitOfWork(), shouldClearChangeTrackerOnRetry: false);
		}
	}

	/// <summary>
	/// <para>
	/// A mock implementation to provide <see cref="DbContextScope"/> instances.
	/// </para>
	/// </summary>
	public class MockDbContextProvider<TContext, TDbContext> : MockDbContextProvider<TDbContext>, IDbContextProvider<TContext>
		where TDbContext : DbContext
	{
		/// <summary>
		/// <para>
		/// Constructs a provider based on empty, non-functional <typeparamref name="TDbContext"/> instances.
		/// The constructor of <typeparamref name="TDbContext"/> will be bypassed altogether.
		/// </para>
		/// <para>
		/// This is intended for tests that do not require a realistic implementation.
		/// </para>
		/// </summary>
		public MockDbContextProvider()
			: base()
		{
		}

		/// <summary>
		/// <para>
		/// Constructs a provider based on empty, non-functional <typeparamref name="TDbContext"/> instances.
		/// The constructor of <typeparamref name="TDbContext"/> will be bypassed altogether.
		/// </para>
		/// <para>
		/// This is intended for tests that do not require a realistic implementation.
		/// </para>
		/// </summary>
		public MockDbContextProvider(DbContextScopeOptions options)
			: base(options)
		{
		}

		/// <summary>
		/// Constructs a provider based on a <typeparamref name="TDbContext"/> factory.
		/// </summary>
		public MockDbContextProvider(Func<TDbContext> factoryMethod, DbContextScopeOptions? options = null)
			: base(factoryMethod, options)
		{
		}

		/// <summary>
		/// Constructs a provider based on a fixed <typeparamref name="TDbContext"/>.
		/// </summary>
		public MockDbContextProvider(TDbContext instance, DbContextScopeOptions? options = null)
			: base(instance, options)
		{
		}

		/// <summary>
		/// This implementation is required to mock the behavior when the compile-time type is <see cref="IDbContextProvider{TContext}"/> of <typeparamref name="TContext"/>
		/// as opposed to of <typeparamref name="TDbContext"/>.
		/// It will redirect to the implementation in <see cref="MockDbContextProvider{TDbContext}"/>.
		/// </summary>
		TResult IDbContextProvider<TContext>.ExecuteInDbContextScope<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, Func<IExecutionScope<TState>, TResult> task)
		{
			return ((IDbContextProvider<TDbContext>)this).ExecuteInDbContextScope(scopeOption, state, task);
		}

		/// <summary>
		/// This implementation is required to mock the behavior when the compile-time type is <see cref="IDbContextProvider{TContext}"/> of <typeparamref name="TContext"/>
		/// as opposed to of <typeparamref name="TDbContext"/>.
		/// It will redirect to the implementation in <see cref="MockDbContextProvider{TDbContext}"/>.
		/// </summary>
		Task<TResult> IDbContextProvider<TContext>.ExecuteInDbContextScopeAsync<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			return ((IDbContextProvider<TDbContext>)this).ExecuteInDbContextScopeAsync(scopeOption, state, cancellationToken, task);
		}
	}
}
