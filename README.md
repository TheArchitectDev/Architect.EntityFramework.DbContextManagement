# Architect.EntityFramework.DbContextManagement

Manage your DbContexts the right way.

- [TLDR](#tldr)
- [Introduction](#introduction)
- [Recommended Use](#recommended-use)
- [Advantages](#advantages)
  * [Advantages of Scoped Execution](#advantages-of-scoped-execution)
- [Deeper Service Hierarchies](#deeper-service-hierarchies)
- [Nested Scopes](#nested-scopes)
  * [Controlling Scope Nesting](#controlling-scope-nesting)
- [Options](#options)
  * [DefaultScopeOption](#defaultscopeoption)
  * [ExecutionStrategyOptions](#executionstrategyoptions)
  * [AvoidFailureOnCommitRetries](#avoidfailureoncommitretries)
- [Internal DbContext Types](#internal-dbcontext-types)
- [Testing the Persistence Layer](#testing-the-persistence-layer)
- [Unit Testing the Orchestrating Layer](#unit-testing-the-orchestrating-layer)
- [Integration Testing the Orchestrating Layer](#integration-testing-the-orchestrating-layer)
  * [Testing Retries](#testing-retries)
- [Connection Resilience](#connection-resilience)
- [Manual Use](#manual-use)

## TLDR

The persistence or infrastructure layer uses the DbContext (e.g. from a repository). Controlling its scope and transaction lifetime, however, is ideally the reponsibility of the orchestrating layer (e.g. from an application service). This package adds that ability to Entity Framework Core 5.0.0 and up.

## Introduction

- Is the lifetime of your DbContext objects clearly visible and easy to control? <em>(Or is there a hidden dependency on `ServiceLifetime.Scoped` in the DI container?)</em>
- Are you free to register your stateless services as singletons if you want to? <em>(Or do they get `ServiceLifetime.Scoped` forced upon them by a DbContext dependency?)</em>
- Is your DbContext lifetime independent of your application type? For example, does the behavior stay the same between ASP.NET, Blazor Server, a console application, and integration tests?
- Are write operations within a unit of work automatically kept within a single database transaction? Even if multiple calls to `SaveChangesAsync` are necessary?
- Is [connection resilience](https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency) (such as with `EnableRetryOnFailure`) automatically applied to each unit of work _as a whole_?
- When using `RowVersion` or `ConcurrencyToken`, is it easy to implement correct retries?

These are common issues when working with Entity Framework. As it turns out, all of them can be easily tackled when DbContext management is solved appropriately. This package provides a single, clean solution for DbContext management and these issues.

The venerable Mehdi El Gueddari explains the benefits of ambiently scoped DbContexts in his long and excellent [post](https://mehdi.me/ambient-dbcontext-in-ef6/) on the subject. However, a truly good and up-to-date implementation was lacking. In fact, such an implementation has the potential to handle many more good practices out-of-the-box.

## Recommended Use

The recommended usage pattern, dubbed "scoped execution", comes with many additional advantages, [outlined below](#advantages-of-scoped-execution).

**Register** the component on startup:

```cs
public void ConfigureServices(IServiceCollection services)
{
   // Register the DbContext with one of the EF 5+ factory-based extensions
   services.AddPooledDbContextFactory<MyDbContext>(context =>
      context.UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure()));

   // Register this library
   services.AddDbContextScope<MyDbContext>();
}
```

**Access** the current DbContext from the persistence layer:

```cs
public class MyRepository : IMyRepository
{
   // This computed property abstracts away how we obtain the DbContext
   private MyDbContext DbContext => this.DbContextAccessor.CurrentDbContext;

   private IDbContextAccessor<MyDbContext> DbContextAccessor { get; }
   
   public OrderRepo(IDbContextAccessor<MyDbContext> dbContextAccessor)
   {
      // Inject an IDbContextAccessor
      this.DbContextAccessor = dbContextAccessor;
   }

   public Task<Order> GetOrderById(long id)
   {
      return this.DbContext.Orders.SingleOrDefaultAsync(o.Id == id);
   }

   public Task AddOrder(Order order)
   {
      return this.DbContext.Orders.AddAsync(order);
   }
}
```

So far, the above would throw, since we have not made a DbContext available.

**Provide** a DbContext from the orchestrating layer (which eventually calls down into the persistence layer, either directly or indirectly):

```cs
public class MyApplicationService
{
   private IDbContextProvider<MyDbContext> DbContextProvider { get; }

   private IMyRepository MyRepository { get; }

   public MyApplicationService(IDbContextProvider<MyDbContext> dbContextProvider, IMyRepository myRepository)
   {
      // Inject an IDbContextProvider
      this.DbContextProvider = dbContextProvider;
      
      this.MyRepository = myRepository;
   }

   public async Task PerformSomeUnitOfWork()
   {
      // Provide a DbContext and execute a block of code within its scope
      await this.DbContextProvider.ExecuteInDbContextScopeAsync(async executionScope =>
      {
         // Until the end of this block, IDbContextAccessor can access the scoped DbContext
         // It can do so from any number of invocations deep
         await this.MyRepository.AddOrder(new Order());
         
         // If we have made modifications, we should save them
         // We could save here or as part of the repository methods, depending on our preference
         await executionScope.DbContext.SaveChangesAsync();
      }); // If the scope was not nested in another, the transaction is committed asynchronously here (unless an exception bubbled up or executionScope.Abort() was called)
   }
}
```

## Advantages

Managing DbContexts with `IDbContextProvider` and `IDbContextAccessor` provides several advantages:

- Stateless repositories are simpler. (We avoid the injection of a DbContext, which is a stateful resource.)
- We can control the DbContext lifetime and transaction boundaries from the place that makes sense, without polluting any methods with extra parameters.
- The DbContext lifetime is easily matched to the transaction boundaries.
- DbContext management is independent of the application type or architecture. (For example, this approach works perfectly with Blazor Server, avoiding its [usual troubles](https://docs.microsoft.com/en-us/aspnet/core/blazor/blazor-server-ef-core?view=aspnetcore-3.1). It also behaves exactly the same in integration tests.)
- Different concrete DbContext subtypes are handled independently.
- A unit of work may be nested. For example, a set of operations may explicitly require being transactional. If there is an encompassing transaction, they can join it; if not, they can have their own transaction.
- It is possible to [keep the DbContext type `internal`](#internal-dbcontext-types) to the persistence layer, without compromising any of the above.

### Advantages of Scoped Execution

Additionally, the recommended [scoped execution](#recommended-use) (as opposed to [manual operation](#manual-use)) handles many good practices for us. It prevents developers from forgetting them, implementing them incorrectly, or having to write boilerplate code for them.

- The unit of work is automatically transactional. Only once the outermost scope ends successfully, the transaction is committed.
- If the work is exclusively read-only, no database transaction is used, avoiding needless overhead.
- If an exception bubbles up from any scope, or `IExecutionScope.Abort()` is called, the entire unit of work fails, and the transaction is rolled back.
   - Further attempts to use the DbContext result in a `TransactionAbortedException`, protecting against inadvertently committing only the second half of a unit of work.
- The DbContext's execution strategy is used. For example, if we use SQL Server with `EnableRetryOnFailure()`, its behavior is applied.
   - This makes it easy to achieve [connection resilience](https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency).
   - Connection resilience is especially important to "serverless" databases, as with Azure SQL's serverless plan.
- Retry behavior is applied at the correct level. For example, if `EnableRetryOnFailure()` causes a retry, then the entire code block is retried with a clean DbContext. This avoids subtle bugs caused by state leakage.
   - Make sure to consider which behavior should be part of the retryable unit. Generally, doing as much as possible _inside_ the scope is more likely to be correct.
   - It is advisable to load, modify, and save within a single scope. A retry will run the entire operation from scratch. This way, domain rules can be validated against the current state if a retry takes place.
   - This behavior is [easily tested](#testing-retries).
- We [avoid](#connection-resilience) the risk of data corruption that Entity Framework causes in case of a failure on commit.
   - When using a retrying execution strategy, Entity Framework would normally [retry even after a failure on commit](https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency#transaction-commit-failure-and-the-idempotency-issue), which could lead to data corruption.
   - This is not fixed in Entity Framework [yet](https://github.com/dotnet/efcore/issues/22904#issuecomment-705743508).
- When using row versions or concurrency tokens for optimistic concurrency, retries can be configured to apply to concurrency conflicts as well, using `ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure`. By loading, modifying, and saving in a single code block, optimistic concurrency conflicts can be handled with zero effort.
   - This behavior is [easily tested](#testing-retries).

## Deeper Service Hierarchies

The `IDbContextAccessor` can access the DbContext provided by the `IDbContextProvider` any number of invocations deep, without the need to pass any parameters.

```cs
// Application service
public async Task PerformSomeUnitOfWork()
{
   await this.DbContextProvider.ExecuteInDbContextScopeAsync(async executionScope =>
   {
      var order = await this.MyDomainService.GetExampleOrder();

      // ...
   });
}

// Domain service
public Task GetExampleOrder()
{
   return this.OrderRepo.GetOrderById(1);
}

// Repository
public Task<Order> GetOrderById(long id)
{
   // As long as it is in scope, the ambient DbContext provided by the IDbContextProvider is visible to the IDbContextAccessor from anywhere
   var dbContext = this.DbContextAccessor.DbContext;

   return dbContext.Orders.SingleOrDefaultAsync(o.Id == id);
}
```

## Nested Scopes

Scopes may be nested. For example, a service may have repository-using behavior that can be invoked on its own. That behavior requires that a DbContext is provided. Perhaps it even requires being executed as a single transaction.

Imagine an outer method that wants to invoke the aforementioned method but make an additional change as part of the same transaction.

Both scenarios above are supported by default, because a scope joins the encompassing scope. If the inner method is invoked on its own, with no encompassing scope, it creates the DbContext at the start and commits the transaction at the end. On the other hand, if the inner method is invoked from the outer method, which has provided an encompassing scope, then the inner scope will merely perform its work as part of the outer scope. It is the outer scope that creates the DbContext and commits the transaction.

```cs
public async Task AddMoneyTransfer(Account account, Transfer transfer)
{
   account.Balance += transfer.Amount;

   await this.DbContextProvider.ExecuteInDbContextScopeAsync(async executionScope =>
   {
      // For demonstration purposes, say that the repository methods invoke SaveChangesAsync()
      await this.AccountRepo.UpdateAndSave(account);
      await this.TransferRepo.AddAndSave(transfer);
   }); // CommitTransactionAsync() is invoked when our scope ends, but only if we are the outermost scope
}

// This methods calls the above one, and does more
public async Task TransferMoney(Account fromAccount, Account toAccount, Transfer transfer)
{
   fromAccount.Balance -= transfer.Amount;

   await this.DbContextProvider.ExecuteInDbContextScopeAsync(async executionScope =>
   {
      // For demonstration purposes, say that the repository methods invoke SaveChangesAsync()
      await this.AccountRepo.UpdateAndSave(fromAccount);

      // This method will use the DbContext we provided, and leave committing the transaction to us
      await this.AddMoneyTransfer(toAccount, transfer);
   }); // CommitTransactionAsync() is invoked when our scope ends, but only if we are the outermost scope
}
```

If any inner scope calls `IExecutionScope.Abort()` or lets an exception bubble up, then the entire ongoing transaction is rolled back, and any further database interaction with the DbContext results in a `TransactionAbortedException`. This helps avoid accidentally committing partial changes from the outer scope, which could otherwise leave the database in an inconsistent state.

### Controlling Scope Nesting

Scope nesting can be controlled by explicitly passing an `AmbientScopeOption` to `ExecuteInDbContextScopeAsync()`, or by [changing the default value](#defaultscopeoption) on registration.

- `AmbientScopeOption.JoinExisting`, the default, causes the encompassing scope to be joined if there is one.
- `AmbientScopeOption.NoNesting` throws an exception if an encompassing scope is present.
- `AmbientScopeOption.ForceCreateNew` obscures any encompassing scopes, pretending they do not exist until the new scope is disposed.

## Options

A number of options can be configured on registration:

```cs
// The defaults are displayed here
services.AddDbContextScope<MyDbContext>(scope => scope
   .DefaultScopeOption(AmbientScopeOption.JoinExisting)
   .ExecutionStrategyOptions(ExecutionStrategyOptions.None)
   .AvoidFailureOnCommitRetries(true));
```

### DefaultScopeOption

[Scope nesting](#controlling-scope-nesting) is controlled by the `AmbientScopeOption`, optionally passed when the `IDbContextProvider` creates a scope. The default value can be configured like this:

```cs
   .DefaultScopeOption(AmbientScopeOption.NoNesting)
```

### ExecutionStrategyOptions

Scoped execution always makes use of the DbContext's configured execution strategy. For example, if we use SQL Server with `EnableRetryOnFailure()`, its behavior is applied.

But we can get more benefits. The format used by scoped execution lends itself perfectly to handling optimistic concurrency conflicts by retrying. This is all that is needed if the retryable unit of work is structured as "load, modify, save".

The usual way to add optimistic concurrency detection is by [concurrency tokens or row versions](https://docs.microsoft.com/en-us/ef/core/modeling/concurrency?tabs=fluent-api). Once they are in place, the following option causes such conflicts to lead to a retry:

```cs
   .ExecutionStrategyOptions(ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure)
```

Furthermore, when this option is used, retries (regardless of their reason) can be tested with [integration tests](#integration-testing-the-orchestrating-layer). By wrapping the `IDbContextProvider<T>` in a `ConcurrencyConflictDbContextProvider<T>`, we get a `DbUpdateConcurrencyException` exception at the _end_ of the outermost task, and only on the first attempt. With `RetryOnOptimisticConcurrencyFailure` enabled, we can test that the result is the same as when no concurrency exceptions were thrown.

For full integration tests that get their dependencies from an `IServiceProvider`, wrapping the `IDbContextProvider<T>` in a `ConcurrencyConflictDbContextProvider<T>` is achieved through `IServiceCollection.AddConcurrencyConflictDbContextProvider()`.

The section on [integration tests](#integration-testing-the-orchestrating-layer) provides code samples.

### AvoidFailureOnCommitRetries

Retrying after a failure on commit is [risky](#connection-resilience). It risks duplicate effects if the original commit turns out to have actually succeeded. By default, in the rare case where a failure on commit is the cause of an exception, scoped execution prevents the retry, letting the exception to bubble up wrapped in an `IOException`.

This recommended feature can be disabled as follows:

```cs
   .AvoidFailureOnCommitRetries(false)
```

## Internal DbContext Types

Sometimes it is desirable to have the visbility of a specific DbContext type set to `internal`, i.e. only visible to its own project. For example, we might have certain internal types that are exposed in `DbSet<T>` properties on the DbContext. Since `DbSet<T>` properties need to have a public getter to function (at least at the time of writing), keeping their types internal requires the DbContext itself to be internal as well.

An internal DbContext type raises the question: How can the orchestrating layer provide a DbContext of a type that it cannot see?

To tackle this, any type can be selected as the _representative_ of the DbContext type.

Consider the following empty interface used to represent `OrderDbContext` in a more implementation-agnostic way:

```cs
/// <summary>
/// Conceptually represents the Order database.
/// </summary>
public interface IOrderDatabase
{
}
```

The interface needs to be visible to both the orchestrating layer and the persistence layer. There may be a utility project that is visible to both, or the domain layer might be suitable. (If you feel uncomfortable admitting to the existing of a database from the domain layer, consider that the business tends to recognize its existence. Alternatively, choose a different set of challenges and stick with a public DbContext type.)

To use `IOrderDatabase` to represent `OrderDbContext`, register the library as follows:

```cs
services.AddDbContextScope<IOrderDatabase, OrderDbContext>();
```

Clearly, the above registration must be made from the project that contains `OrderDbContext`. This is not surprising: with an internal DbContext, the DbContext itself must _also_ be registered from the project that contains it. The recommended approach is to equip the project with an extension method that allows its dependencies to be registered by outer projects:

```cs
public static IServiceCollection AddDatabaseInfrastructure(this IServiceCollection services)
{
   // DbContext
   services.AddPooledDbContextFactory<OrderDbContext>(context => context.UseSqlite("Filename=:memory:"));

   // Scoped DbContext management
   services.AddDbContextScope<IOrderDatabase, OrderDbContext>(scope =>
      scope.ExecutionStrategyOptions(ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure));

   // Repositories
   services.AddSingleton<IOrderRepo, OrderRepo>();
   // ...

   return services;
}
```

The persistence layer still injects an `IDbContextAccessor<OrderDbContext>` as normal. The orchestrating layer, however, can now control things with an `IDbContextProvider<IOrderDatabase>` - note the generic type argument.

## Testing the Persistence Layer

When testing the layer that actually uses the DbContext, we will have a dependency on `IDbContextAccessor`. The implementation may use that dependency to try to obtain the DbContext.

The dependency is easily fulfilled and controlled with a custom implementation:

```cs
var dbContextAccessor = FixedDbContextAccessor.Create(myDbContext);
var repo = new MyRepo(dbContextAccessor);
```

If a container is used for the tests, then the dependency can be _registered_ instead:

```cs
var dbContextAccessor = FixedDbContextAccessor.Create(myDbContext);
hostBuilder.ConfigureServices(services =>
   services.AddSingleton<IDbContextAccessor<MyDbContext>>(dbContextAccessor));
```

## Unit Testing the Orchestrating Layer

When we write unit tests on the orchestrating layer, the persistence code will be mocked out. As such, `IDbContextAccessor` will not be needed. However, the orchestrating layer will still have a dependency on `IDbContextProvider`. Moreover, if scoped execution is used, the flow of execution should resemble the production scenario.

The package provides a `MockDbContextProvider`, which makes it easy to satisfy the dependency while still providing the original flow of execution.

If we have a DbContext [factory] available, we can do this:

```cs
var dbContextProvider = new MockDbContextProvider<MyDbContext>(myDbContextOrDbContextFactory);
var applicationService = new ApplicationService(dbContextProvider, myRepo);
```

However, in unit tests, we generally want to avoid letting the DbContext connect to a database. To do so, the recommendation is to use the repository pattern (or any Inversion-of-Control (IoC) pattern) to mock out any queries and calls to `Add` and `AddRange`.

We can have the `MockDbContextProvider` automatically create dummy DbContext instances:

```cs
// Alternative with directly used DbContext
var dbContextProvider = new MockDbContextProvider<OrderDbContext>();
// Alternative with indirectly represented DbContext
var dbContextProvider = new MockDbContextProvider<IOrderDatabase, OrderDbContext>();

var applicationService = new ApplicationService(dbContextProvider, myRepo);
```

It should be noted that `SaveChanges` throws if the parameterless `MockDbContextProvider` constructor was used. We can cause `SaveChanges` to work by pretending to use a real connection, provided that no entities are added to the change tracker:

```cs
// Pretend to use a real connection, so that SaveChanges will succeed as long as no entities are added to the change tracker
// The connection string merely needs to pass some initial validation, not actually work
// Example using SQL Server
var dbContextProvider = new MockDbContextProvider<OrderDbContext>(() => new OrderDbContext(
   new DbContextOptionsBuilder<OrderDbContext>().UseSqlServer(@"Data Source=;").Options));

// ...

this.OrderRepo.Add(order); // Mocked out, to avoid adding entities to the change tracker

// SaveChanges() will succeed as long as the change tracker is empty, because no connection will be opened
await executionScope.DbContext.SaveChangesAsync(cancellationToken);

// Potential manual calls to CommitTransaction() can be made conditional
// The execution scope will have started no transaction if SaveChanges() had nothing to save
if (dbContext.Database.CurrentTransaction is not null)
   await executionScope.DbContext.Database.CommitTransactionAsync(cancellationToken);

// Test methods could verify that IOrderRepo.Add was invoked, for example
```

## Integration Testing the Orchestrating Layer

To run integration tests that include database interaction, the recommendation is to use LocalDB and/or docker containers.

Below is an example of how to set up an integration test with Entity Framework. Note that the example works well regardless of whether the current package is used.

```cs
/// <summary>
/// Integration tests on the OrderApplicationService.
/// </summary>
public class OrderApplicationServiceTests : IDisposable
{
   /// <summary>
   /// Used to run a test method in an IHost, with a DI container.
   /// </summary>
   private HostBuilder HostBuilder { get; } = new HostBuilder();

   /// <summary>
   /// Lazily resolved, so that test methods can modify the container last-minute.
   /// </summary>
   private IHost Host => this._host ??= this.CreateHost();
   private IHost? _host;

   /// <summary>
   /// The subject under test.
   /// </summary>
   private OrderApplicationService ApplicationService =>
      this.Host.Services.GetRequiredService<OrderApplicationService>();

   /// <summary>
   /// Test method setup.
   /// </summary>
   public OrderApplicationServiceTests()
   {
      // For some reason Entity Framework uses "first registration wins" instead of "last registration wins"
      // So configure the test DbContext FIRST
      // Alternatively, the production code's registration logic can be used, with the connection string changed via configuration
      this.HostBuilder.ConfigureServices(services =>
         services.AddPooledDbContextFactory<OrderDbContext>(
            context => context.UseSqlServer(@"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True;Pooling=False;Connect Timeout=15;", sqlServer => sqlServer.EnableRetryOnFailure())));

      // Call the method that registers the application's dependencies
      this.HostBuilder.ConfigureServices(services => services.AddOrderApplication());
   }

   /// <summary>
   /// Test method teardown.
   /// </summary>
   public void Dispose()
   {
      this._host?.Dispose();
   }

   private IHost CreateHost()
   {
      var host = this.HostBuilder.Build();

      using var dbContext = host.Services.GetRequiredService<IDbContextFactory<OrderDbContext>>().CreateDbContext();
      dbContext.Database.EnsureCreated();

      return host;
   }
   
   /// <summary>
   /// Creates a DbContext instance to be used for test setup and assertions.
   /// </summary>
   private Task<OrderDbContext> CreateDbContextAsync()
   {
       return this.Host.Services.GetRequiredService<IDbContextFactory<OrderDbContext>>().CreateDbContextAsync();
   }

   /// <summary>
   /// An example test method.
   /// </summary>
   [Fact]
   public async Task AddOrderAsync_Regularly_ShouldHaveExpectedEffect()
   {
      // Arrange

      const int customerId = 1;

      await using var dbContext = await this.CreateDbContextAsync();

      dbContext.Set<Customers>().Add(new Customer(customerId));
      await dbContext.SaveChangesAsync();

      // Ensure that entities are reloaded after acting
      dbContext.ChangeTracker.Clear();

      // Act

      await this.ApplicationService.AddOrderAsync(customerId, amount: 1.23m);

      // Assert

      var orders = await dbContext.Set<Order>().ToListAsync();
      var order = Assert.Single(orders);
      Assert.Equal(customerId, order.CustomerId);
      Assert.Equal(1.23m, order.Amount);

      var customer = await dbContext.Set<Customer>().SingleAsync();
      Assert.Equal(1, customer.OrderCount);
   }

   // ...
}
```

### Testing Retries

Additionally, when `ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure` is used, the retry behavior can be tested with integration tests. Doing so is recommended, since retries are more prone to bugs.

```cs
// Run once with false and once with true
[Theory]
[InlineData(false)]
[InlineData(true)]
public async Task GetOrderShippingStatusAsync_WithExistingOrderAndPossibleRetryAttempt_ShouldReturnExpectedResult(
   bool withConcurrencyException)
{
   // If withConcurrencyException=true,
   // the first invocation of ExecuteInDbContextScopeAsync()
   // will throw a DbUpdateConcurrencyException just before committing, and then retry
   if (withConcurrencyException)
      this.HostBuilder.ConfigureServices(services => services
         .AddConcurrencyConflictDbContextProvider<MyDbContext>());

   var result = await this.ApplicationService.GetOrderShippingStatusAsync(orderId: 1);

   // With or without retry, the result should be as expected
   // Snip: assertions
}
```

## Connection Resilience

Entity Framework's [connection resilience guidelines](https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency) are highly recommended and work perfectly - if not more easily - with this package. Scoped execution provides the most suitable format for connection resilience.

On issue that deserves highlighting is [failure on commit](https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency#transaction-commit-failure-and-the-idempotency-issue). When a connection failure happens during transaction commit, the commit might succeed while the application loses the connection. The application cannot know whether it needs to retry or not. Entity Framework _does_ retry. **This scenario is very unlikely, but also very hard to counter.**

To avoid the risk of duplicate inserts and updates, this package prevents Entity Framework from retrying if an exception occurs _during transaction commit_. (This applies only to the execution scope's own commit action. Manual commit calls are not affected.) This is a safer default than the one Entity Framework provides.

As such, the recommended approach is to _not_ handle failure on commit. Allow it to throw, and produce an internal server error to indicate the uncertain result. Failure on commit is highly unlikely, so avoiding incorrect results is usually sufficient. (By contrast, implementing _correct_ retries, by querying whether the transaction was successfully committed, would require a custom implementation _for each method_.)

If a method _does_ warrant a fully retrying implementation even for failure on commit, an approach like the following can be used:

- Just before the end of the execution scope, commit manually, within a try/catch block.
- If an exception is caught, start a separate unit of work, using scope option `ForceCreateNew`. Use this to manually query the result of the transaction.
- If the commit was successful, return normally.
- If the commit was unsuccessful, initiate a retry by throwing an exception of a type that will be retried by the provider. If `ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure` is used, throwing a `DbUpdateConcurrencyException` will have the same effect.

## Manual Use

If we merely want to control the DbContext, without the [advantages](#advantages-of-scoped-execution) provided by [scoped execution](#recommended-use), we can.

Note that the manual approach merely provides a DbContext and lets it be accessed. The application code is responsible for transactions, the use of execution strategies, retries, etc.

**Register** the component and **access** the DbContext according to the [recommended use](#recommended-use), but **provide** the DbContext from the orchestrating layer as follows:

```cs
public class MyApplicationService
{
   private IDbContextProvider<MyDbContext> DbContextProvider { get; }
   private IMyRepository MyRepository { get; }

   public MyApplicationService(IDbContextProvider<MyDbContext> dbContextProvider, IMyRepository myRepository)
   {
      // Inject an IDbContextProvider
      this.DbContextProvider = dbContextProvider;

      this.MyRepository = myRepository;
   }

   public async Task PerformSomeUnitOfWork()
   {
      // Make a DbContext available until the scope is disposed
      await using var dbContextScope = this.DbContextProvider.CreateDbContextScope();

      // IDbContextAccessor can access the scoped DbContext
      // It can do so from any number of invocations deep
      await this.MyRepository.AddOrder(new Order());

      // If we made modifications, we should save them
      // This example chooses to save here rather than in the repository, but either way works
      await executionScope.DbContext.SaveChangesAsync();
   }
}
```
