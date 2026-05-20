```mermaid
sequenceDiagram
    participant Client
    participant API as Minimal API

    Client->>API: GET /health
    note over API: [RequireRateLimiting("fixed")] — 429 Too Many Requests if limit exceeded
    note over API: Not proxied through Varnish — accessed directly for health monitoring
    API-->>Client: 200 OK { status: "healthy", timestamp: "..." }
```

**Notes:**
- No database or external service call — response is computed in-process
- Intended for health probes (load balancer, Docker healthcheck, monitoring) — direct access on port 5000 bypasses Varnish intentionally
