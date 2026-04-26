# Architecture

## Current state

### Components

```mermaid
graph TD
    Client["HTTP Client"]
    API["EventsController (ASP.NET Core)"]
    CommentsAPI["CommentsController (ASP.NET Core)"]
    Service["EventService (Domain)"]
    Cache["CachedEventRepository (Decorator)"]
    Repo["SqlServerEventRepository (Dapper)"]
    CommentRepo["MongoDbCommentRepository"]
    Redis[("Redis")]
    SQL[("SQL Server")]
    Mongo[("MongoDB")]

    Client --> ErrorHandler
    ErrorHandler["ErrorHandlingMiddleware"] --> API
    ErrorHandler --> CommentsAPI
    API --> Service
    Service --> Cache
    Cache -->|Cache hit| Redis
    Cache -->|Cache miss| Repo
    Repo --> SQL
    CommentsAPI --> CommentRepo
    CommentRepo --> Mongo
```

### Clean Architecture layers

| Layer | Project | Responsibility |
|-------|---------|----------------|
| API | `EventManager.Api` | Controllers, validators, middleware, configuration |
| Domain | `EventManager.Domain` | Entities, interfaces, DTOs, services, exceptions |
| Infrastructure | `EventManager.Infrastructure` | Repositories, data access, cache |

**Error handling:** `ErrorHandlingMiddleware` intercepts all unhandled exceptions before they reach the client. It logs the full details server-side (exception type, message, stack trace, requestId) and returns a minimal response — no internal details exposed in production.

Project dependency diagram

```mermaid
graph LR
    API --> Domain
    Infrastructure --> Domain
```

---

## Target

### Components

```mermaid
graph TD
    Client["HTTP Client"]
    Varnish["Varnish (HTTP Cache)"]
    API["EventsController (ASP.NET Core)"]
    Service["EventService (Domain)"]
    Cache["CachedEventRepository (Decorator)"]
    Repo["SqlServerEventRepository (Dapper)"]
    CommentRepo["CommentRepository (MongoDb)"]
    SearchService["EventSearchService"]
    Redis[("Redis")]
    SQL[("SQL Server")]
    Mongo[("MongoDB")]
    ES[("Elasticsearch")]

    Client --> Varnish
    Varnish --> API
    API --> Service
    Service --> Cache
    Cache -->|Cache hit| Redis
    Cache -->|Cache miss| Repo
    Repo --> SQL
    API --> CommentRepo
    CommentRepo --> Mongo
    API --> SearchService
    SearchService --> ES
```

---

## Data flows

### GET /api/events?page=&size=

see [Get events sequence diagram](.\flows\GET-events.md)

### GET /api/events/{id}

see [Get event sequence diagram](.\flows\GET-event.md)

### POST /api/events

see [POST event sequence diagram](.\flows\POST-event.md)

### GET /api/events/{id}/comments

see [GET comments sequence diagram](.\flows\GET-comments.md)

### POST /api/events/{id}/comments

see [POST comment sequence diagram](.\flows\POST-comment.md)

---

## Technical decisions

| Technology | Role | Justification |
|------------|------|---------------|
| SQL Server | Event data | Structured data, ACID constraints |
| Redis | Application cache | Configurable TTL, fine-grained key invalidation |
| MongoDB | Comments | Semi-structured data, free text |
| Elasticsearch | Search | Full-text, per-field boost, relevance scoring |
| Varnish | HTTP cache | Transparent caching of full GET responses |
