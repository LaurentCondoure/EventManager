```mermaid
sequenceDiagram
    participant Client
    participant Varnish
    participant EH as ExceptionHandler
    participant API as EventsController
    participant Service as EventService
    participant Search as EventSearchService
    participant ES as Elasticsearch

    Client->>Varnish: GET /api/events/search?q=jazz&page=1
    note over Varnish: Pass-through — search results are never cached by Varnish
    Varnish->>EH: GET /api/events/search?q=jazz&page=1
    EH->>API: GET /api/events/search?q=jazz&page=1
    note over API: [EnableRateLimiting("fixed")] — 429 Too Many Requests if limit exceeded
    alt Missing or empty q parameter
        API-->>Client: 400 Bad Request
    else q parameter present
        API->>Service: SearchAsync(query, page, pageSize)
        Service->>Search: SearchAsync(query, page, pageSize)
        Search->>ES: multi-match query (title^2, description, category, artistName)
        alt Elasticsearch error
            ES-->>EH: Exception
            EH-->>Client: 500 Internal Server Error
        else Elasticsearch ok
            ES-->>Search: scored results (sorted by _score desc, date asc)
            Search-->>Service: IEnumerable<SearchResultDto>
            Service-->>API: IEnumerable<SearchResultDto>
            API-->>Client: 200 OK (empty array if no results)
        end
    end
```

**Notes:**
- Search results are not cached at the application level (no Redis layer) — Elasticsearch response latency is acceptable for the current volume
- Varnish pass-through is intentional: search results change as events are indexed or updated, and vary by query string — HTTP-level caching would require `Vary: *`, which defeats the purpose
- Title field is boosted ×2 relative to description, category, and artistName — a search for "jazz" ranks events with "jazz" in the title above those with "jazz" only in the description
