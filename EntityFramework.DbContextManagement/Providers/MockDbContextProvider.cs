using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement.DbContextScopes;
using Architect.EntityFramework.DbContextManagement.Dummies;
using Architect.EntityFramework.DbContextManagement.ExecutionStrategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// A mock implementation to provide <see cref="DbContextScope"/> instances.
	/// </para>
	/// </summary>
	public class MockDbContextProvider<TContext, TDbContext> : DbContextProvider<TContext, TDbContext>
		where TDbContext : DbContext
	{
		private static readonly ConditionalWeakTable<UnitOfWork, DummyUnitOfWork> DummyUnitsOfWorkByOriginalUnitsOfWork =
			new ConditionalWeakTable<UnitOfWork, DummyUnitOfWork>();

		public override DbContextScopeOptions Options { get; }

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

		public override DbContextScope CreateDbContextScope(AmbientScopeOption? scopeOption = null)
		{
			var result = DbContextScope.Create(this.DbContextFactory, scopeOption ?? this.Options.DefaultScopeOption, this.Options);
			return result;
		}

		protected IExecutionStrategy CreateDummyExecutionStrategy(DbContext dbContext)
		{
			// Since we provide mock behavior, do not proceed into the DbContext's own strategy
			return new DummyExecutionStrategy(dbContext);
		}

		protected override IExecutionStrategy GetExecutionStrategyFromDbContext(DbContext dbContext)
		{
			return this.CreateDummyExecutionStrategy(dbContext);
		}

		public override TResult ExecuteInDbContextScope<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, Func<IExecutionScope<TState>, TResult> task)
		{
			return TransactionalStrategyExecutor.ExecuteInDbContextScopeAsync(
				this as IDbContextProvider<TContext>, scopeOption, state, default, ExecuteSynchronously,
				async: false,
				getUnitOfWork: dbContextScope => GetDummyUnitOfWorkForOriginalUnitOfWork(dbContextScope.UnitOfWork),
				shouldClearChangeTrackerOnRetry: false)
				.RequireCompleted();

			// Local function that executes the given task and returns a completed task
			Task<TResult> ExecuteSynchronously(IExecutionScope<TState> executionScope, CancellationToken _)
			{
				var result = task(executionScope);
				return Task.FromResult(result);
			}
		}

		public override Task<TResult> ExecuteInDbContextScopeAsync<TState, TResult>(
			AmbientScopeOption scopeOption,
			TState state, CancellationToken cancellationToken, Func<IExecutionScope<TState>, CancellationToken, Task<TResult>> task)
		{
			return TransactionalStrategyExecutor.ExecuteInDbContextScopeAsync(
				this as IDbContextProvider<TContext>, scopeOption, state, cancellationToken, task,
				async: true,
				getUnitOfWork: dbContextScope => GetDummyUnitOfWorkForOriginalUnitOfWork(dbContextScope.UnitOfWork),
				shouldClearChangeTrackerOnRetry: false);
		}

		/// <summary>
		/// Lets us provide our own <see cref="DummyUnitOfWork"/>, but still scoped in accordance with the original one.
		/// </summary>
		private static DummyUnitOfWork GetDummyUnitOfWorkForOriginalUnitOfWork(UnitOfWork original)
		{
			return DummyUnitsOfWorkByOriginalUnitsOfWork.GetOrCreateValue(original);
		}
	}

	/// <summary>
	/// <para>
	/// A mock implementation to provide <see cref="DbContextScope"/> instances.
	/// </para>
	/// </summary>
	public class MockDbContextProvider<TDbContext> : MockDbContextProvider<TDbContext, TDbContext>
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
	}
}
