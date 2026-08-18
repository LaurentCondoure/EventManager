# Design — Event Management Flows

**Reference:** DESIGN-V0-001
**Status:** Validated
**Version:** V0 — POC
**Date:** 2026-04-19

> This document captures the technical flows and architecture diagrams for the Event Management domain.
> It describes how components interact on critical paths.
> For technology choices, see the ADRs referenced in [index.md](../adr/index.md).
> For API contracts, see [api-events.md](../../api/api-events.md).

---

## Target Architecture

```mermaid
graph TB
    Client["Web Client (Vue.js 3)"]
    Varnish["Varnish (HTTP Cache — 5min TTL)"]
    API["API .NET 8 (ASP.NET Core)"]
    Redis["Redis (Applicative Cache)"]
    SQL["SQL Server (Structured Data)"]
    Mongo["MongoDB (Comments)"]
    ES["Elasticsearch (Full-text Search)"]

    Client -->|HTTP| Varnish
    Varnish -->|on MISS| API
    API --> Redis
    API --> SQL
    API --> Mongo
    API --> ES
```

---

## Data Flows

### GET /api/events — Paginated event list

```mermaid
sequenceDiagram
    participant Client
    participant Varnish
    participant API
    participant Redis
    participant SQL as SQL Server

    Client->>Varnish: GET /api/events
    alt Varnish HIT
        Varnish-->>Client: 200 OK (cached)
    else Varnish MISS
        Varnish->>API: GET /api/events
        API->>Redis: check "events:page:1:size:20"
        alt Redis HIT
            Redis-->>API: cached result
        else Redis MISS
            API->>SQL: SELECT events WHERE date >= today
            alt SQL error
                SQL-->>API: error
                API-->>Client: 500 Internal Server Error
            else SQL ok
                SQL-->>API: events
                API->>Redis: store (TTL 10min)
                API-->>Varnish: 200 OK
                Varnish->>Varnish: store (TTL 5min)
                Varnish-->>Client: 200 OK
            end
        end
    end
```

**Design notes:**
- Varnish intercepts repeated identical HTTP requests before they reach the API — zero application cost on cache hit
- Redis caches the SQL result as a .NET object — avoids database round-trip on subsequent API calls
- Two cache levels serve different purposes: Varnish absorbs HTTP traffic, Redis absorbs database load

---

### GET /api/events/{id} — Event detail

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Redis
    participant SQL as SQL Server

    Client->>API: GET /api/events/{id}
    API->>Redis: check "event:{id}"
    alt Redis HIT
        Redis-->>API: cached result
        API-->>Client: 200 OK
    else Redis MISS
        API->>SQL: SELECT event WHERE id = {id}
        alt not found
            SQL-->>API: null
            API-->>Client: 404 Not Found
        else SQL error
            SQL-->>API: error
            API-->>Client: 500 Internal Server Error
        else found
            SQL-->>API: event
            API->>Redis: store (TTL 10min)
            API-->>Client: 200 OK
        end
    end
```

---

### POST /api/events — Create event

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant SQL as SQL Server
    participant ES as Elasticsearch
    participant Redis

    Client->>API: POST /api/events
    alt validation fails
        API-->>Client: 400 Bad Request
    else validation ok
        API->>SQL: INSERT event
        alt SQL error
            SQL-->>API: error
            API-->>Client: 500 Internal Server Error
        else SQL ok
            SQL-->>API: ok
            API->>ES: index event
            note over API,ES: ES failure is ignored (MVP) — see ADR-004
            API->>Redis: invalidate "events:page:*"
            API-->>Client: 201 Created
        end
    end
```

**Design notes:**
- SQL Server is the source of truth — written first
- Elasticsearch is indexed synchronously in the same handler — search results are immediately consistent with the database
- Redis cache is invalidated on write — next GET /api/events will reflect the new event
- If Elasticsearch indexing fails, the event is still created — search may be temporarily inconsistent (accepted for MVP, see ADR-004)

---

### GET /api/events/search?q={query} — Full-text search

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Redis
    participant ES as Elasticsearch

    Client->>API: GET /api/events/search?q=jazz
    alt missing query param
        API-->>Client: 400 Bad Request
    else query ok
        API->>Redis: check "search:jazz:page:1"
        alt Redis HIT
            Redis-->>API: cached results
            API-->>Client: 200 OK
        else Redis MISS
            API->>ES: multi-match query (title^2, description, category, artistName)
            alt ES error
                ES-->>API: error
                API-->>Client: 500 Internal Server Error
            else ES ok
                ES-->>API: scored results
                API->>Redis: store (TTL 10min)
                API-->>Client: 200 OK
            end
        end
    end
```

**Design notes:**
- Elasticsearch handles full-text scoring and tokenization — SQL Server `LIKE '%keyword%'` does not provide relevance ranking
- Redis caches frequent search queries — Elasticsearch queries are more expensive than SQL reads

---

### GET /api/events/{id}/comments — Event comments

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant SQL as SQL Server
    participant Mongo as MongoDB

    Client->>API: GET /api/events/{id}/comments
    API->>SQL: SELECT event WHERE id = {id}
    alt event not found
        SQL-->>API: null
        API-->>Client: 404 Not Found
    else event found
        API->>Mongo: find by eventId, sort createdAt desc
        alt MongoDB error
            Mongo-->>API: error
            API-->>Client: 500 Internal Server Error
        else ok
            Mongo-->>API: comments (empty array if none)
            API-->>Client: 200 OK
        end
    end
```

**Design notes:**
- Event existence is verified in SQL Server before querying MongoDB — avoids orphan comment queries
- No cache layer — comments are user-generated, change frequently, and consistency is expected

---

### POST /api/events/{id}/comments — Add comment

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant SQL as SQL Server
    participant Mongo as MongoDB

    Client->>API: POST /api/events/{id}/comments
    alt validation fails
        API-->>Client: 400 Bad Request
    else validation ok
        API->>SQL: SELECT event WHERE id = {eventId}
        alt event not found
            SQL-->>API: null
            API-->>Client: 404 Not Found
        else event found
            API->>Mongo: insert comment
            alt MongoDB error
                Mongo-->>API: error
                API-->>Client: 500 Internal Server Error
            else ok
                Mongo-->>API: ObjectId
                API-->>Client: 201 Created
            end
        end
    end
```

---

## Cache Scenarios

### Scenario A — Applicative cache (Redis)

```
1. GET /api/events  →  MISS (~50ms)
2. GET /api/events  →  HIT (~5ms)
3. POST /api/events →  cache invalidated
4. GET /api/events  →  MISS, new event present (~50ms)
5. GET /api/events  →  HIT (~5ms)
```

### Scenario B — HTTP cache (Varnish)

```
1. GET http://localhost:8080/api/events  →  X-Cache: MISS (~50ms)
2. GET http://localhost:8080/api/events  →  X-Cache: HIT (~2ms)
3. Wait 5 minutes (TTL expired)
4. GET http://localhost:8080/api/events  →  X-Cache: MISS (~50ms)
```

### Scenario C — Double cache layer

```
1. GET /api/events (via Varnish)  →  Varnish MISS → Redis MISS → SQL  (~50ms)
2. GET /api/events (via Varnish)  →  Varnish HIT                       (~2ms)
3. POST /api/events (port 5000)   →  SQL + ES + Redis invalidation
4. GET /api/events after 5min     →  Varnish MISS → Redis MISS → SQL  (~50ms)
5. GET /api/events (via Varnish)  →  Varnish HIT                       (~2ms)
```

---

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | 2026-04-19 | Document created from design.md |
