# xUnit — Test Isolation and Lifecycle

## The problem: shared state between tests

When multiple tests share a resource (database, Elasticsearch index, Redis...) via `IClassFixture`, data written by one test persists for the next. This can cause false failures.

**Concrete example encountered in this project:**

`ReindexAllAsync_ShouldReplaceIndex` indexes events with `Category = "Concert"` and leaves them in the shared index. When `DeleteAsync_ShouldRemoveEvent_FromIndex` runs next and searches for `"Concert à supprimer"`, the word `"Concert"` matches the `category` field of the leftover events — the search returns results instead of an empty list.

---

## xUnit's design philosophy

xUnit deliberately removed the equivalents of NUnit's `[SetUp]` / `[TearDown]` or MSTest's `[TestInitialize]` / `[TestCleanup]`. The rationale: these hooks encourage shared mutable state between tests, which is exactly the source of contamination problems.

Instead, xUnit relies on two principles:

### 1. One instance per test

xUnit creates a **new instance** of the test class for each test method. This is a fundamental xUnit rule, unlike NUnit or MSTest which reuse the same instance.

```
Instance 1 → constructor → test ReindexAllAsync → instance destroyed
Instance 2 → constructor → test DeleteAsync     → instance destroyed
Instance 3 → constructor → test IndexAsync      → instance destroyed
```

This provides instance-level isolation — but it doesn't clean up external resources (an Elasticsearch index, a database...) between tests.

### 2. Fixtures for shared resources

`IClassFixture<T>` creates the fixture **once** for the entire test class and injects the same instance into each test class instance. This is intentional: starting a Docker container takes 15-20s — you don't want to repeat that for each test.

---

## The solution: `IAsyncLifetime`

For async setup/teardown (network calls, I/O, Docker containers...), xUnit provides `IAsyncLifetime`. Implementing it on the **test class** (not the fixture) adds a `BeforeEach` / `AfterEach` around each test:

```csharp
public class EventSearchServiceTests : IClassFixture<ElasticsearchFixture>, IAsyncLifetime
{
    private readonly EventSearchService _sut;
    private readonly ElasticsearchFixture _fixture;

    public EventSearchServiceTests(ElasticsearchFixture fixture)
    {
        _fixture = fixture;
        _sut = new EventSearchService(fixture.Client);
    }

    // Runs after the constructor, before each test
    public async ValueTask InitializeAsync()
    {
        try { await _fixture.Client.Indices.DeleteAsync("events", TestContext.Current.CancellationToken); }
        catch { /* index does not exist yet on the first test */ }
    }

    // Runs after each test, before the instance is destroyed
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

**Execution flow with `IAsyncLifetime`:**

```
Instance 1 → constructor → InitializeAsync (delete index) → test → DisposeAsync
Instance 2 → constructor → InitializeAsync (delete index) → test → DisposeAsync
Instance 3 → constructor → InitializeAsync (delete index) → test → DisposeAsync
```

The container stays alive for the entire class (economical), but the index is wiped between each test (isolated).

---

## Summary

| Mechanism | Scope | Use case |
|---|---|---|
| Constructor / `Dispose` | Per test instance | Synchronous setup (in-memory fakes, mocks) |
| `IAsyncLifetime` on test class | Per test (BeforeEach / AfterEach) | Async cleanup between tests (index, table, cache) |
| `IClassFixture<T>` | Per test class (shared) | Expensive resources (Docker containers) |
| `ICollectionFixture<T>` | Per test collection (shared) | Resources shared across multiple test classes |

`IAsyncLifetime` is not a workaround — it is xUnit's official solution for async setup/teardown. There is no other hook mechanism in xUnit by design.
