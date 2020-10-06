# Architect.EntityFramework.DbContextManagement

Manage your DbContexts the right way.

The data access layer or infrastructure layer uses the DbContext (e.g. from a repository). The proper scope and transaction lifetime, however, is ideally controlled by the orchestrating layer (e.g. from an application service). This package adds that ability to Entity Framework Core 5.0.0 and up.

The venerable Mehdi El Gueddari explains the benefits of this approach in his long and excellent [post](https://mehdi.me/ambient-dbcontext-in-ef6/). However, a truly good _implementation_ was lacking. Furthermore, such an implementation has the potential to handle many more good practices out-of-the-box.

### Recommended Use

The recommended usage pattern comes with many [additional advantages](#advantages-of-scoped-execution).

Register the component on startup:

```cs
public void ConfigureServices(IServiceCollection services)
{
	// Register your DbContext with one of EF 5.0.0's new factory-based extensions
	services.AddPooledDbContextFactory<MyDbContext>(context => context.UseSqlite("Filename=:memory:"));
	
	// Register this library
	services.AddDbContextScope<MyDbContext>();
}
```

Access the current DbContext from the data access layer:

```cs
public class MyRepository : IMyRepository
{
	// This computed property makes it easy for us to get the DbContext from the IDbContextAccessor
	private MyDbContext DbContext => this.DbContextAccessor.CurrentDbContext;
	
	private IDbContextAccessor<MyDbContext> DbContextAccessor { get; }
	
	public OrderRepo(IDbContextAccessor<MyDbContext> dbContextAccessor)
	{
		// Inject an IDbContextAccessor
		this.DbContextAccessor = dbContextAccessor ?? throw new ArgumentNullException(nameof(dbContextAccessor));
	}
	
	public Task<Order> GetOrderById(long id)
	{
		return this.DbContext.Orders.SingleOrDefaultAsync(o.Id == id);
	}
	
	public Task AddOrder(order)
	{
		return this.DbContext.Orders.AddAsync(order);
	}
}
```

So far, the above would throw, since we have not made a DbContext available.

From the orchestrating layer, which eventually leads to the invocation of a repository method, provide a DbContext:

```cs
public class MyApplicationService
{
	private IDbContextProvider<MyDbContext> DbContextProvider { get; }
	private MyRepository MyRepository { get; }

	public MyApplicationService(IDbContextProvider<MyDbContext> dbContextProvider, MyRepository myRepository)
	{
		// Inject an IDbContextProvider
		this.DbContextProvider = dbContextProvider ?? throw new ArgumentNullException(nameof(dbContextProvider));
		
		this.MyRepository = myRepository ?? throw new ArgumentNullException(nameof(myRepository));
	}

	public async Task PerformSomeUnitOfWork()
	{
		await this.DbContextProvider.ExecuteInDbContextScopeAsync(async executionScope =>
		{
			// Until the end of this block, IDbContextAccessor can access the scoped DbContext
			// It can do so from any number of invocations deep (not shown here)
			await this.MyRepository.AddOrder(new Order());
			
			// If we made modifications, we should save them
			// We could save here or as part of the repository methods, depending on our preference
			await executionScope.DbContext.SaveChangesAsync();
		}); // If no exceptions occurred and this scope was not nested in another, the transaction is committed asynchronously here
	}
}
```

### Advantages

- Stateless repositories are simpler. (We avoid the injection of a DbContext, which is a stateful resource.)
- We can control the DbContext lifetime and transaction boundaries from the place that makes sense, without polluting methods with extra parameters.
- The DbContext lifetime easily matches the transaction boundaries.
- DbContext management is independent of the application type or architecture. (For example, this approach works perfectly with Blazor Server, avoiding its [usual troubles](https://docs.microsoft.com/en-us/aspnet/core/blazor/blazor-server-ef-core?view=aspnetcore-3.1). It also behaves exactly the same in integration tests.)
- Multiple DbContext subtypes are handled independently.

In addition, [scoped execution](#recommended-use) handles many good practices for us. It prevents developers from forgetting them, implementing them incorrectly, and having to write boilerplate code for them.

- The unit of work is automatically transactional. Only once the outermost scope ends successfully, the transaction is committed.
- If the work is exclusively read-only, no database transaction is started, avoiding needless overhead.
- If an exception bubbles up from any scope, or `IExecutionScope.Abort()` is called, the entire unit of work fails, and the transaction is rolled back.
- The DbContext's execution strategy is honored. For example, if we use SQL Server with `EnableRetryOnFailure()`, its behavior is applied.
	- This makes it easy to achieve [connection resilience](https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency).
	- Connection resilience is especially important to "serverless" databases, as with Azure SQL's serverless plan.
- Retry behavior is applied at the correct level. For example, if `EnableRetryOnFailure()` causes a retry, then the entire code block is retried with a clean DbContext. This avoids subtle bugs caused by state leakage.
	- Make sure to consider which behavior should be part of the retryable unit. Generally, doing as much as possible _inside_ the scope is more likely to be correct.
	- It is advisable to load, modify, and save within a single scope. A retry will run the entire operation from scratch, taking into account any changes when domain rules are validated once more.
	- This behavior is easily [tested](#TODO) with the help of `MockDbContextProvider<TDbContext>.ScopedExecutionThrowsConcurrencyException`.
- When using row versions or concurrency tokens for optimistic concurrency, retries can be configured to apply to concurrency conflicts as well, using `ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure`. By loading, modifying, and saving in a single code block, optimistic concurrency conflicts can be handled with zero effort.
	- This behavior is easily [tested](#TODO) with the help of `MockDbContextProvider<TDbContext>.ScopedExecutionThrowsConcurrencyException`.

### Deeper Service Hierarchies

The `IDbContextAccessor` can access the DbContext provided by the `IDbContextProvider` any number of invocations deep, without the need to pass any parameters.

```cs
// Application service
public async Task PerformSomeUnitOfWork()
{
	await this.DbContextProvider.ExecuteInDbContextScopeAsync(async executionScope =>
	{
		var order = await this.MyDomainService.GetExampleOrder();
		
		// ...
		
		executionScope.Complete();
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

### Nested Scopes

Scopes may be nested. For example, a service may have repository-using behavior that can be invoked on its own. That behavior requires that a DbContext is provided. Perhaps it even requires being executed as a single transaction.

Imagine an outer method that wants to invoke the aforementioned method but make an additional change as part of the same transaction.

Both scenarios above are supported by default, because a scope joins an encompassing scope by default. If the inner method is invoked on its own, with no encompassing scope, it creates the DbContext at start and commits the transaction at the end. However, if the inner method is invoked from the outer method, which has provided an encompassing scope, then the inner scope will merely perform its work as part of the outer scope. It is the outer scope that creates the DbContext and commits the transaction.

```cs
public async Task AddMoneyTransfer(Account account, Transfer transfer)
{
	account.Balance += transfer.Amount;
	
	await this.DbContextProvider.ExecuteInDbContextScopeAsync(async executionScope =>
	{
		// For demonstration purposes, say that the repository methods invoke SaveChangesAsync()
		await this.AccountRepo.UpdateAndSave(account);
		await this.TransferRepo.AddAndSave(transfer);
		
		executionScope.Complete();
	});
}

public async Task TransferMoney(Account fromAccount, Account toAccount, Transfer transfer)
{
	fromAccount.Balance -= transfer.Amount;
	
	await this.DbContextProvider.ExecuteInDbContextScopeAsync(async executionScope =>
	{
		// For demonstration purposes, say that the repository methods invoke SaveChangesAsync()
		await this.AccountRepo.UpdateAndSave(fromAccount);
		
		// This method will use the DbContext we provided, and leave committing the transaction to us
		await this.AddMoneyTransfer(toAccount, transfer);
		
		executionScope.Complete();
	}); // CommitTransactionAsync() is invoked when our scope ends, if we are the outermost scope
}
```

If any inner scope fails to call `IExecutionScope.Complete()`, then any ongoing the transaction is rolled back, and any further database interaction with the DbContext results in a `TransactionAbortedException`. This helps avoid accidentally committing partial changes, which could have left the database in an invalid state.

#### Controlling Scope Nesting

Scope nesting can be controlled by explicitly passing an `AmbientScopeOption` to `ExecuteInDbContextScopeAsync()`, or by [changing](#defaultscopeoption) the default value on registration.

`AmbientScopeOption.JoinExisting`, the default, causes the encompassing scope to be joined if there is one.

`AmbientScopeOption.NoNesting` throws an exception if an encompassing scope is present.

`AmbientScopeOption.ForceCreateNew` obscures any encompassing scopes, pretending they do not exist until the new scope is disposed.

### Options

### ExecutionStrategyOptions

// #TODO: Decide on default and document it here

Scoped execution always makes use of the DbContext's configured execution strategy. For example, if we use SQL Server with `EnableRetryOnFailure()`, its behavior is applied.

But we can get more benefits. Scoped execution's format lends itself perfectly to handling optimistic concurrency conflicts by retrying. The usual way to add optimistic concurrency detection is by [concurrency tokens or row versions](https://docs.microsoft.com/en-us/ef/core/modeling/concurrency?tabs=fluent-api). Once this is in place, the following option causes such conflicts to lead to a retry:

```cs
	.ExecutionStrategyOptions(ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure)
```

Furthermore, when this option is used, retries (regardless of their reason) can be tested with [integration tests](#integration-testing-the-orchestrating-layer). By wrapping the `IDbContextProvider<T>` in an `ConcurrencyConflictDbContextProvider<T>`, a `DbUpdateConcurrencyException` except is thrown at the _end_ of the outermost task, and only on the first attempt. With `RetryOnOptimisticConcurrencyFailure` enabled, we can test that the result is the same as when no concurrency exceptions were thrown.

For full integration tests that get their dependencies from an `IServiceProvider`, the `IDbContextProvider<T>` can be wrapped in a `ConcurrencyConflictDbContextProvider<T>` through `IServiceCollection.AddConcurrencyConflictDbContextProvider`.

#### DefaultScopeOption

[Scope nesting](#controlling-scope-nesting) is controlled by the `AmbientScopeOption` passed when the `IDbContextProvider` creates a scope. The default value can be configured like this:

```cs
	.DefaultScopeOption(AmbientScopeOption.NoNesting)
```

### Testing the Data Access Layer

### Unit Testing the Orchestrating Layer

### Integration Testing the Orchestrating Layer

### Manual Use

If you merely want to control the DbContext, without the [advantages](#advantages-of-scoped-execution) provided by [scoped execution](#recommended-use), you can.

Follow the [recommended use](#recommended-use), but implement the orchestrating layer as follows:

```cs
public class MyApplicationService
{
	private IDbContextProvider<MyDbContext> DbContextProvider { get; }
	private MyRepository MyRepository { get; }

	public MyApplicationService(IDbContextProvider<MyDbContext> dbContextProvider, MyRepository myRepository)
	{
		// Inject an IDbContextProvider
		this.DbContextProvider = dbContextProvider ?? throw new ArgumentNullException(nameof(dbContextProvider));
		
		this.MyRepository = myRepository ?? throw new ArgumentNullException(nameof(myRepository));
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

With the manual approach, the application code is responsible for transactions, execution strategies, etc.





- Your [orchestrating] services must be in control of the business transaction boundary.
- Your [orchestrating] services must be in control of the database transaction scope and isolation level.
- The way your DbContext is managed should be independent of the architecture of the application.
- The way your DbContext is managed should be independent of the application type.
- Your DbContext management strategy should support multiple DbContext-derived types.
- Your DbContext management strategy should work with [Entity Framework's] async workflow.

In addition to the above, this package offers the following benefits:

- Modifications are automatically transactional, committing on explicit success or rolling back otherwise. (Note that this comes at no additional cost, since without this, EF does the same thing _per invocation_ of `SaveChanges`.)

- A unit of work may be nested. A set of operations may explicitly require being transactional, while being able to participate in an encompassing transaction.

- `SaveChanges` may be invoked from either the orchestrating layer or the data access layer, depending on your preference.

- If you want to keep your DbContext subclass internal to the data access layer, this is possible without compromise to any of the above.










##### Requirements

- The orchestrating service controls the DbContext's effective lifetime, allowing it to be aligned with the business transaction.
- We can work well with multiple concrete DbContext types.
	- <em>Some applications use multiple DbContext types, e.g. to work with different databases.</em>
- We can manage DbContexts in a high-concurrency environment.
	- <em>Note that a DbContext itself is not thread-safe, but a high-traffic environment may have many threads working on different ones that must not interfere with each other.</em>

##### Easy-of-use API additional requirements

- Changes are saved at the end, and no operations can be done on the DbContext after that.
	- <em>This avoids many subtle pitfalls.</em>
	- <em>Within the current transaction, if we request an entity that we have made changes to, we will still see them, thanks to the DbContext's identity map.</em> // #TODO: Confirm this holds true with tracking disabled
- The configured execution strategy is honored.
	- <em>Execution strategies can be used to achieve [connection resilience](https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency).</em>
	- <em>Connection resilience is even more important to "serverless" databases, such as Azure SQL's serverless plan. They database may have been paused, requiring wake-up-and-retry.</em>
- Since we already require a retryable form to achieve connection resilience, we can use that to automate optimistic concurrency (opt-in).
	- <em>If the entity is updated from elsewhere after we loaded it but before we saved it, we will get a `DbUpdateConcurrencyException`.</em>
	- <em>If our implementation overwrites _only_ the entity's properties that were _actually_ changed, then we can automate optimistic concurrency by simply retrying the entire unit of work: load, change, save.</em>
	- <em>Loading gives us the current version of the entity, including the changes from the competing update. Our own logic should overwrite only the properties that we wish to change. Saving writes only the changes, keeping any other changes from the competing update intact.</em>
