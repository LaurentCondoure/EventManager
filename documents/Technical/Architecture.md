# Architecture

## Current state

### Components

```mermaid
graph TD
    Client["HTTP Client"]
    API["EventsController (ASP.NET Core)"]
    Service["EventService (Domain)"]
    Cache["CachedEventRepository (Decorator)"]
    Repo["EventRepository (Dapper)"]
    Redis[("Redis")]
    SQL[("SQL Server")]

    Client --> API
    API --> Service
    Service --> Cache
    Cache -->|Cache hit| Redis
    Cache -->|Cache miss| Repo
    Repo --> SQL
```

### Clean Architecture layers

| Layer | Project | Responsibility |
|-------|---------|----------------|
| API | `EventManager.Api` | Controllers, validators, middleware, configuration |
| Domain | `EventManager.Domain` | Entities, interfaces, DTOs, services, exceptions |
| Infrastructure | `EventManager.Infrastructure` | Repositories, data access, cache |

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

---

## Technical decisions

| Technology | Role | Justification |
|------------|------|---------------|
| SQL Server | Event data | Structured data, ACID constraints |
| Redis | Application cache | Configurable TTL, fine-grained key invalidation |
| MongoDB | Comments | Semi-structured data, free text |
| Elasticsearch | Search | Full-text, per-field boost, relevance scoring |
| Varnish | HTTP cache | Transparent caching of full GET responses |
