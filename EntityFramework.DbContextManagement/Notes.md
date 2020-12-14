Have you ever asked yourself the following questions?

- Is the lifetime of your DbContext objects clearly visible and easy to control? <em>(Or is there a hidden dependency on Scoped lifetime in the DI container?)</em>
- Are you free to register your stateless services as singletons if you want to? <em>(Or are they restricted by injected DbContexts with a Scoped lifetime?)</em>
- Is your DbContext lifetime independent of your application type? For example, does the behavior stay the same between ASP.NET, Blazor Server, a console application, and integration tests?
- Are write operations within a unit of work automatically kept within one database transaction? Even if multiple calls to `SaveChangesAsync` are necessary?
- Is [connection resilience](https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency) (such as with `EnableRetryOnFailure`) automatically applied to each unit of work as a whole?
- When using `RowVersion` or `ConcurrencyToken`, is it easy to implement correct retries?

If you are using Entity Framework, the answer to most of these is likely "no". Luckily, we can do better.
