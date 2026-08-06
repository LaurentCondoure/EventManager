# Elasticsearch — Concepts et Implémentation

**Author:** Laurent Condoure
**Date:** 2026-07-03  
**Status:** Draft
**Project:** EventManager — Cultural Events Management Application  
**Objective:** Introduces Elasticsearch and describes how it's used in the application.

## What is Elasticsearch

Elasticsearch is a distributed, open-source search and analytics engine built on Apache Lucene.
It stores data as JSON documents in **indices** and exposes a REST API to index, search, and aggregate them.

Unlike SQL Server (structured, relational) or MongoDB (semi-structured documents), Elasticsearch is optimised for **full-text search**: it parses text into tokens, builds inverted indexes, and ranks results by relevance score.

```mermaid
flowchart TD
    Q["User query: 'jazz paris'"] --> ES[Elasticsearch]
    ES --> J["'jazz' → doc-2, doc-7, doc-12"]
    ES --> P["'paris' → doc-2, doc-9"]
    J --> I["Intersection + relevance score"]
    P --> I
    I --> R["doc-2 (score 1.8), doc-7 (score 0.6), ..."]
```

### Why Elasticsearch for this project

SQL Server `LIKE '%jazz%'` does not scale — it scans every row, ignores typos, and has no relevance ranking.
Elasticsearch provides:

| Feature | Benefit |
|---|---|
| Full-text analysis | Tokenisation, stemming, stop words |
| Relevance scoring (BM25) | Results ordered by pertinence |
| Field boosting | Title matches outweigh description matches |
| Pagination | `from` / `size` native |
| Near real-time indexing | Documents searchable within ~1 second |

---

## Architecture

### A Java server behind a REST API

Elasticsearch is a Java server (JVM) built on **Lucene**, a full-text search library. Lucene manages the on-disk index files; Elasticsearch adds the HTTP REST API on top, along with index/replica management and, in a cluster, distribution of data across nodes — none of which come into play here, since this project runs a single dev node (`discovery.type=single-node`).

```mermaid
flowchart TD
    Code[".NET code"] -->|"HTTP POST /events/_search"| SDK["ElasticsearchClient (SDK)"]
    SDK -->|"TCP :9200"| ES
    subgraph Docker["Docker container"]
        ES["Elasticsearch (JVM)"] --> Idx["'events' index — Lucene files on disk"]
    end
```

### The .NET client is a JSON/HTTP wrapper

`Elastic.Clients.Elasticsearch` builds and sends plain HTTP requests — there is no custom binary protocol underneath. The fluent C# query:

```csharp
_client.SearchAsync<EventSearchDocument>(s => s
    .Index("events")
    .From(0)
    .Size(20)
    .Query(q => q.MultiMatch(m => m.Query("jazz")))
)
```

is translated by the SDK into:

```json
POST /events/_search
{
  "from": 0,
  "size": 20,
  "query": {
    "multi_match": {
      "query": "jazz",
      "fields": ["title^2", "description", "category", "artistName"]
    }
  }
}
```

This matters for debugging: the fastest way to check what is actually being sent or stored is the raw JSON — via `curl` against `:9200`, or a Dev Tools-style console such as Kibana's. Kibana is not part of this project's `docker-compose.yml`; it is mentioned here as a common companion tool in production Elasticsearch setups, not as something already configured.

### Request flow

```mermaid
flowchart TD
    ClientPost["POST /api/events"] --> Controller[EventsController]
    ClientSearch["GET /api/events/search?q=jazz"] --> Controller
    Controller --> Service[EventService]

    Service -->|"CreateAsync / UpdateAsync"| Repo[IEventRepository]
    Service -->|"DeleteAsync"| Repo
    Service -->|"IndexAsync / DeleteAsync"| Search[IEventSearchService]
    Service -->|"SearchAsync"| Search

    Repo --> SQL[("SQL Server<br/>source of truth")]
    Search -->|"HTTP :9200"| ES[("Elasticsearch<br/>'events' index — denormalized copy")]
```

---

## Docker Setup

```yaml
elasticsearch:
  image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
  environment:
    - discovery.type=single-node
    - xpack.security.enabled=false
  ports:
    - "9200:9200"
```

`discovery.type=single-node` disables the cluster formation protocol — required for a standalone dev instance.

---

## Authentication and TLS

Elasticsearch 8.0+ activates **X-Pack Security by default** — authentication and TLS both come enabled out of the box on a fresh install.

### Local development (this project)

Both are disabled in `docker-compose.yml`:

```yaml
environment:
  - xpack.security.enabled=false   # disables authentication and TLS together
```

- No credentials required
- Connection string: `http://localhost:9200`
- No user secrets needed

Containers talk over Docker's internal network here — acceptable to leave TLS off for a closed, local dev environment. It would not be an acceptable default anywhere else.

```json
// appsettings.json
"Elasticsearch": {
  "Url": "http://localhost:9200"
}
```

`docker-compose.yml` also sets `ES_JAVA_OPTS: "-Xms512m -Xmx512m"`, capping the JVM heap at 512MB for local dev — Elasticsearch's own default minimum is 1GB.

### Production (or local with auth enabled)

Credentials are embedded in the URL and stored in **user secrets** (never in `appsettings.json`):

```
http://elastic:yourpassword@localhost:9200
```

```bash
dotnet user-secrets set "Elasticsearch:Url" "http://elastic:yourpassword@localhost:9200"
```

The `ElasticsearchClientSettings` picks up the credentials from the URL automatically — no extra configuration needed.

Enabling TLS outside Docker also requires certificates:

```yaml
# elasticsearch.yml
xpack.security.enabled: true
xpack.security.http.ssl.enabled: true
xpack.security.http.ssl.keystore.path: certs/http.p12
```

— which then means generating, distributing, and renewing them over time.

### Self-hosted vs. managed service

A managed service (e.g. Elastic Cloud) trades that operational burden for cost and vendor lock-in:

```
https://user:password@my-cluster.es.io:9243
```

TLS is active with no configuration — the provider manages certificates and renewal.

| Concern | Self-hosted | Managed service |
|---|---|---|
| TLS / certificates | Generate, configure, renew | Connection string, that's it |
| High availability | Configure replicas | Included by default |
| Backups | Script + monitoring | Automatic |
| Scaling | Add nodes | A slider in the console |

---

## .NET Client

**Package:** `Elastic.Clients.Elasticsearch` (version 9.3.5)

This is the official Elastic .NET client. It replaces the older NEST library (v7).

```xml
<PackageReference Include="Elastic.Clients.Elasticsearch" Version="9.3.5" />
```

### Concrete class — no public interface

`ElasticsearchClient` is a concrete class. It does **not** implement a public interface.
This is why the abstraction lives in the domain layer as `IEventSearchService`, not at the client level.

### Product check (EC.E 9.x)

On every HTTP response, the client validates the `X-Elastic-Product: Elasticsearch` header.
Without it, `UnsupportedProductException` is thrown. This mechanism prevents accidentally connecting to an incompatible server (e.g. OpenSearch).

---

## Configuration and DI Registration

### `ElasticsearchOptions`

```csharp
public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";
    public string Url { get; init; } = string.Empty;
}
```

### Registration in `Program.cs`

```csharp
builder.Services.Configure<ElasticsearchOptions>(
    builder.Configuration.GetSection(ElasticsearchOptions.SectionName));

builder.Services.AddSingleton<ElasticsearchClient>(sp =>
{
    var url = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value.Url;
    var settings = new ElasticsearchClientSettings(new Uri(url));
    return new ElasticsearchClient(settings);
});

builder.Services.AddScoped<IEventSearchService, EventSearchService>();
```

`ElasticsearchClient` is registered as **singleton**: the client manages its own connection pool internally and is designed to be shared across the application lifetime — the same reasoning already applied to `IMongoClient` (see `MONGODB.md`) and `IConnectionMultiplexer` (see `Redis.md`).

---

## Index Document

`EventSearchDocument` is the shape of the data stored in the `events` Elasticsearch index.
It is distinct from the `Event` domain entity and the `EventDto` — it contains only the fields relevant to search.

```csharp
public class EventSearchDocument
{
    public Guid   Id          { get; set; }
    public string Title       { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime Date      { get; set; }
    public string Location    { get; set; } = default!;
    public decimal Price      { get; set; }
    public string Category    { get; set; } = default!;
    public string? ArtistName { get; set; }
}
```

`Capacity` and `CreatedAt` are deliberately absent: they are not searchable fields.

```mermaid
flowchart LR
    subgraph SQL["SQL Server — Events"]
        S_Id[Id]
        S_Title[Title]
        S_Desc[Description]
        S_Date[Date]
        S_Loc[Location]
        S_Cat[Category]
        S_Artist[ArtistName]
        S_Price[Price]
        S_Cap[Capacity]
        S_Created[CreatedAt]
        S_Updated[UpdatedAt]
    end
    subgraph ES["Elasticsearch — 'events' index"]
        E_Id[Id]
        E_Title["Title (boost x2)"]
        E_Desc[Description]
        E_Date[Date]
        E_Loc[Location]
        E_Cat[Category]
        E_Artist[ArtistName]
        E_Price[Price]
    end
    S_Id --> E_Id
    S_Title --> E_Title
    S_Desc --> E_Desc
    S_Date --> E_Date
    S_Loc --> E_Loc
    S_Cat --> E_Cat
    S_Artist --> E_Artist
    S_Price --> E_Price

    classDef excluded fill:#eee,stroke:#999,color:#888,stroke-dasharray: 3 3
    class S_Cap,S_Created,S_Updated excluded
```

`Capacity`, `CreatedAt`, and `UpdatedAt` (greyed out) have no outgoing arrow — they stay in SQL Server only, not useful for search.

The index itself is never created explicitly — it comes into existence implicitly, via Elasticsearch's dynamic mapping, on the first `IndexAsync` call. Tracked as technical debt in `ROADMAP.md` (Architecture) ahead of V1.

### Why a dedicated document class

The search document could theoretically reuse the `Event` entity. A dedicated class was preferred because:
- The index schema can evolve independently from the SQL schema
- Fields irrelevant to search (`Capacity`, audit timestamps) stay out of the index
- The mapping intent is explicit at the type level

---

## Interface

`IEventSearchService` is declared in the **Domain layer** to keep the domain independent of the Elasticsearch infrastructure:

```csharp
public interface IEventSearchService
{
    Task IndexAsync(Event @event);
    Task DeleteAsync(Guid eventId);
    Task<IEnumerable<EventDto>> SearchAsync(string query, int page = 1, int pageSize = 20);
}
```

`ReindexAllAsync` is implemented in `EventSearchService` but not exposed on the interface —
it is an administrative operation, not part of the domain contract.

```mermaid
flowchart TD
    I["IEventSearchService<br/>(Domain layer)"] --> S["EventSearchService<br/>(Infrastructure layer)"]
    S --> C["ElasticsearchClient<br/>(SDK — Singleton)"]
```

---

## Dual Write

SQL Server is the source of truth; Elasticsearch holds a denormalized copy optimised for search only:

```mermaid
flowchart LR
    Post["POST /api/events"] --> S1["1. SQL Server<br/>source of truth — every field"]
    S1 --> S2["2. Elasticsearch<br/>search copy — queryable fields only"]

    Get["GET /api/events/search?q=jazz"] --> S3["Elasticsearch only"]
```

Every create, update, or delete writes to both — that's the **dual write**. The two stores can diverge if the second write fails after the first already succeeded; see Reindex below for the recovery path.

---

## Implementation — `EventSearchService`

### IndexAsync

Maps `Event` → `EventSearchDocument` then calls the ES index API:

```csharp
public async Task IndexAsync(Event @event)
{
    var document = new EventSearchDocument
    {
        Id          = @event.Id,
        Title       = @event.Title,
        Description = @event.Description,
        Date        = @event.Date,
        Location    = @event.Location,
        Price       = @event.Price,
        Category    = @event.Category,
        ArtistName  = @event.ArtistName
    };

    await _client.IndexAsync(document, i => i.Index(IndexName).Id(@event.Id.ToString()));
}
```

### DeleteAsync

Removes a document from the index by its ID:

```csharp
public async Task DeleteAsync(Guid eventId)
{
    await _client.DeleteAsync(IndexName, eventId.ToString());
}
```

### SearchAsync — Multi-match query with boost

```csharp
public async Task<IEnumerable<EventDto>> SearchAsync(string query, int page = 1, int pageSize = 20)
{
    var response = await _client.SearchAsync<EventSearchDocument>(s => s
        .Indices(IndexName)
        .From((page - 1) * pageSize)
        .Size(pageSize)
        .Query(q => q
            .MultiMatch(m => m
                .Query(query)
                .Fields(new[]
                {
                    "title^2",      // title match counts double
                    "description",
                    "category",
                    "artistName"
                })
            )
        )
    );

    return response.Documents.Select(d => new EventDto(
        d.Id, d.Title, d.Description, d.Date, d.Location,
        0, d.Price, d.Category, d.ArtistName,
        DateTime.MinValue, null));
}
```

#### Field boosting

`title^2` means a match in `Title` contributes twice as much to the relevance score as a match in `Description`, `Category`, or `ArtistName`. Results are automatically sorted by descending score.

#### Pagination

`From` maps to the `from` ES parameter (skip N documents). `Size` maps to `size` (return at most N documents). For page 3 with 20 results per page: `from = 40`, `size = 20`.

#### Known limitation

`Capacity` and `CreatedAt` are not available in search results — the search document does not include them. These fields are set to `0` and `DateTime.MinValue` respectively in the returned `EventDto`. If they are needed in the UI after a search, a second call to `GET /api/events/{id}` is required.

### ReindexAllAsync

Administrative method used to rebuild the entire index from scratch (e.g. after a schema change). It is triggered manually via a dedicated Minimal API endpoint — there is no automatic drift detection. *(The source code comment above this endpoint cites "ADR 011" — that ADR is actually about Redis/Varnish cache invalidation, unrelated to Elasticsearch; no ADR currently documents this reindex decision.)*

```mermaid
flowchart LR
    Admin["POST /admin/search/reindex"] --> Service["EventService.ReindexAsync()"]
    Service -->|"GetAllAsync(1, int.MaxValue)"| SQL[("SQL Server")]
    Service -->|"ReindexAllAsync(events)"| ES[("Elasticsearch<br/>'events' index")]
```

`EventService.ReindexAsync()` reads every event back from SQL Server (paginated, but requested in one page of `int.MaxValue`) before handing them to `EventSearchService.ReindexAllAsync`:

```csharp
public async Task ReindexAllAsync(IEnumerable<Event> events)
{
    // 1. Delete all existing documents
    await _client.DeleteByQueryAsync<EventSearchDocument>(IndexName, d => d
        .Query(q => q.MatchAll(new MatchAllQuery())));

    // 2. Bulk index all events
    var documents = events.Select(e => new EventSearchDocument { ... });
    await _client.BulkAsync(b => b.Index(IndexName).IndexMany(documents));
}
```

`BulkAsync` sends every document in a single HTTP request — far more efficient than N individual `IndexAsync` calls.

#### Current implementation has a downtime window

```mermaid
sequenceDiagram
    participant App as ReindexAllAsync()
    participant ES as Elasticsearch index
    participant User as Search request

    App->>ES: 1. DeleteByQuery (empty the index)
    Note over ES,User: downtime window
    User->>ES: GET /search?q=...
    ES-->>User: [] (0 results)
    App->>ES: 2. BulkAsync (reindex everything)
    Note over ES,User: search functional again
```

If Elasticsearch indexing fails after a SQL Server write, the two stores diverge — SQL Server is always written first, so the data itself is never lost, and a reindex repairs Elasticsearch. But `ReindexAllAsync` as implemented empties the index before repopulating it: search returns zero results for every event, not just the affected one, for the duration of the bulk reindex. For the current index size this window is short, but it is a real gap, not a theoretical one.

#### Production alternative — not implemented — blue/green index via alias

An Elasticsearch **alias** can point to different underlying indices, and the target can be swapped atomically:

```mermaid
flowchart TD
    Start["Alias 'events' → events_v1 (live)"] --> Step1["1. Create events_v2<br/>empty, not exposed to clients"]
    Step1 --> Step2["2. BulkAsync into events_v2<br/>zero impact on production traffic"]
    Step2 --> Step3["3. Atomically repoint alias<br/>events → events_v2 — instant, transparent"]
    Step3 --> Step4["4. Delete events_v1"]
    Step4 --> End["Alias 'events' → events_v2 (live)"]
```

The .NET client always queries the alias `events` — it never knows whether `events_v1` or `events_v2` is actually serving it behind the scenes. Zero downtime, at the cost of managing two index generations during the swap.

---

## Testing Strategy

### Why not unit tests

`ElasticsearchClient` is a sealed concrete class with no public interface. It cannot be mocked via Moq.

`Elastic.Transport` 0.16.0 (the transport layer used by EC.E 9.3.5) provides `InMemoryRequestInvoker` to intercept HTTP calls. However, custom response headers passed to its constructor do not reach `ApiCallDetails.ParsedHeaders` in this version — which means the EC.E product check (`X-Elastic-Product: Elasticsearch`) always fails, throwing `UnsupportedProductException`.

Attempting to bypass the product check at the settings level revealed no public configuration option in EC.E 9.x.

### What IS unit-tested

`IEventSearchService` is the abstraction. Any service that **consumes** `IEventSearchService` is unit-tested by mocking the interface:

```csharp
var searchMock = new Mock<IEventSearchService>();
searchMock.Setup(s => s.SearchAsync("jazz", 1, 20)).ReturnsAsync([...]);
```

### EventSearchService belongs to component tests

`EventSearchService` is tested at the **component level** using a real Elasticsearch instance (via Testcontainers or a shared dev instance). See `TEST_STRATEGY.md`.
