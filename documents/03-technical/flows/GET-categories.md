```mermaid
sequenceDiagram
    participant Client
    participant Varnish
    participant EH as ExceptionHandler
    participant API as Minimal API

    Client->>Varnish: GET /api/events/categories
    note over Varnish: Pass-through — not in cached paths
    Varnish->>EH: GET /api/events/categories
    EH->>API: GET /api/events/categories
    note over API: [RequireRateLimiting("fixed")] — 429 Too Many Requests if limit exceeded
    API-->>Client: 200 OK ["Concert", "Théâtre", "Exposition", "Conférence", "Spectacle", "Autre"]
```

**Notes:**
- No database call — `EventCategories.All` is a static in-memory constant defined in `EventManager.Domain`
- Categories are the single source of truth for validation (FluentValidation) and for the frontend dropdown — see ADR-009
