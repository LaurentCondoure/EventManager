```mermaid
sequenceDiagram
    participant Client
    participant API as Minimal API

    Client->>API: GET /health (JWT access token cookie, or X-Api-Key header)
    note over API: [RequireRateLimiting("fixed")] — 429 Too Many Requests if limit exceeded
    note over API: ADR-021 — requires either scheme; OR semantics, not both
    alt Neither a valid JWT cookie nor a valid X-Api-Key header
        API-->>Client: 401 Unauthorized
    else Valid JWT access token cookie (staff session — any authenticated role)
        API-->>Client: 200 OK { status: "healthy", timestamp: "..." }
    else Valid X-Api-Key header (system/automated caller)
        API-->>Client: 200 OK { status: "healthy", timestamp: "..." }
    end
```

**Notes:**
- No database or external service call — response is computed in-process
- Requires authentication (ADR-021): a valid JWT access token cookie **or** the static system
  key in the `X-Api-Key` header — no longer callable anonymously
- The JWT path lets an admin or super admin check status manually (useful during issue
  investigation); the API key path is for a future automated caller and needs no login/cookie
  flow
- No RBAC role restriction yet — any authenticated role is accepted on the JWT path (ADR-021,
  accepted limitations)
- Not proxied through Varnish — accessed directly on port 5000
- Not currently wired to any external liveness probe (no container `HEALTHCHECK` or load
  balancer polls it as of V1) — if one is added later and cannot hold a credential, that needs
  a separate, deliberately public endpoint rather than reopening this one (ADR-021)
