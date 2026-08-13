# Security

## System protection

### RateLimiter
#### limit applied to the API endpoints
Rate limiting applied globally (100 req/min fixed policy via `.RequireRateLimiting("fixed")`), limiting the rate at which abusive queries can be submitted.

#### Known limitations

Fixed time window. The limit can be exceeded if a significant number of requests are sent within the period renewal interval.

---

### Elasticsearch 

#### Search input (`GET /api/events/search?q=`)
The `q` parameter is passed directly to an Elasticsearch `multi_match` query via the official `Elastic.Clients.Elasticsearch` client. The client serializes the query as a structured object — user input is treated as a value, not as Query DSL. Elasticsearch injection is not possible with this approach (equivalent to a parameterized SQL query).

**No input validation is applied on `q`** (length, character set). A malicious or accidental very long string will be forwarded to Elasticsearch as-is.

#### Known limitations

| Endpoint | Parameter | Risk | Planned fix |
|---|---|---|---|
| `GET /api/events/search` | `q` | Unbounded string length forwarded to Elasticsearch | `SearchQueryValidator` — max length + not empty |
| `GET /api/events/search` | `page`, `pageSize` | No upper bound on `pageSize` | Include in `SearchQueryValidator` |

---

### Redis
#### Database protection

Redis cache absorbs repeated identical requests on API endpoints reached through GET HTTP Verb (`GET /api/events`, `GET /api/events/{id}`, `GET /api/events/{id}/full`) before they reach SQL Server.
The consequence is to preserve the SQL Server database in case of repeated consultation requests.

#### Known limitations

- Via versioned keys, any modifications or deletions of events invalidate the cache again. The cache-aside pattern retrieves data from the database in case of a cache miss. If there are too many deletion or modification requests, the protection is useless. In the worst case, this can also impact performance due to exchanges with Redis in addition to the database.

- The search endpoint bypasses Redis and hits Elasticsearch directly.

- MongoDb is not protected either 

## Protection against injections

### SQL Injections

- **SQL injection** — all SQL queries use Dapper parameterization.
- **NoSQL injection (MongoDB)** — all queries use the MongoDB driver with typed filter builders.

### Html/Javascript injections

- **XSS** — API returns JSON only; no HTML rendering server-side.

#### Search injections

- **Elasticsearch Query DSL injection** — `multi_match` treats input as a plain value  and serialized as value, not as query to run; which would have been the case with `query_string` (which interprets Lucene syntax).

---

## Authentication

### Known limitations

The API has no authentication layer. All endpoints are publicly accessible. CORS is configured but only restricts browser-based cross-origin requests — direct calls (curl, Postman, server-to-server) bypass it entirely.

The `/admin/search/reindex` endpoint is particularly sensitive as it triggers a full SQL Server read and Elasticsearch rewrite.

| Limitation | Planned fix |
|---|---|
| No authentication on any endpoint | JWT authentication via `[Authorize]` on all endpoints |
| `/admin/search/reindex` publicly accessible | `[Authorize(Roles = "Admin")]` in addition to JWT |
