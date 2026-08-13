```mermaid
sequenceDiagram
    participant Client
    participant Varnish
    participant EH as ExceptionHandler
    participant API as Minimal API
    participant Service as EventService
    participant Cache as CachedEventRepository
    participant DB as SQL Server
    participant Search as EventSearchService
    participant ES as Elasticsearch

    Client->>Varnish: POST /admin/search/reindex
    note over Varnish: Pass-through — POST is never cached
    Varnish->>EH: POST /admin/search/reindex
    EH->>API: POST /admin/search/reindex
    note over API: [RequireRateLimiting("fixed")] — 429 Too Many Requests if limit exceeded
    API->>Service: ReindexAsync()
    Service->>Cache: GetAllAsync(page=1, pageSize=int.MaxValue)
    Cache->>DB: SELECT * FROM Events WHERE Date >= NOW() (no OFFSET — full table)
    alt DB error
        DB-->>EH: Exception
        EH-->>Client: 500 Internal Server Error
    else DB ok
        DB-->>Cache: IEnumerable<Event> (all events)
        Cache-->>Service: IEnumerable<Event>
        Service->>Search: ReindexAllAsync(events)
        note over Search,ES: Indexes each event individually — bulk API considered for V2 at scale
        alt Elasticsearch error
            ES-->>EH: Exception
            EH-->>Client: 500 Internal Server Error
        else Elasticsearch ok
            ES-->>Search: All documents indexed
            Search-->>Service: ok
            Service-->>API: ok
            API-->>Client: 200 OK
        end
    end
```

**When to use:**
- SQL Server and Elasticsearch have diverged (e.g., after a failed indexing operation, manual DB correction, or data migration)
- Not part of normal operation — each mutation (POST/PUT/DELETE) maintains the index incrementally

**Known limitation:**
- `Results.Ok()` return value is discarded in the handler (`MinimalApiEndpoints.cs` line 21) — the endpoint returns 200 implicitly via Minimal API void semantics, but the intent is not explicit. Low-risk in practice; noted for V2 cleanup.
- `pageSize: int.MaxValue` bypasses the standard 50-item page cap — acceptable for an admin operation; not exposed to end users
