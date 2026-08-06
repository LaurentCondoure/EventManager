# Roadmap

## In progress

---
## Planned versions

## V0 — Sandbox: self-training result
**Business**: event management (CRUD), full-text search, comments
**Stack**: ASP.NET Core .NET 8, Vue.js 3, SQL Server, MongoDB,
Redis, Elasticsearch, Varnish, Docker

## V1.0 — From sandbox to production: security patch + cloud hosting
**Business**: role-based access control (organizer, spectator),
public catalogue, reservations, visual identity
**Stack**: Keycloak (SSO), Nginx, role-based endpoint security,
SolidJS (audience-facing), Reservation API (.NET 8),
PostgreSQL, Azure Service Bus, Azure Front Door,
Azure Container Apps, Azure Container Registry,
Azure DevOps, Terraform Azure

## V1.1 — Personalization Engine
**Business**: user preferences, behavioral tracking (RGPD consent),
seasonal and contextual event recommendations
**Stack**: Python, FastAPI, scikit-learn, pgvector (PostgreSQL)

## V1.2 — Professional artists
**Business**: artist management, cancellations,
rescheduling and refund rules
**Stack**: Angular, NgRx, RxJS

## V1.3 — Venue administration
**Business**: venue management, fill rate feedback loop
**Stack**: React, Redux Toolkit,
ReservationCreated → on-premise flow

## Versions content

### V1.0 — From sandbox to production: security patch + cloud hosting

**Why this milestone**:
V1.0 was a local sandbox — no authentication, no real deployment,
endpoints wide open. V2.0 fixes what makes V1.0 unacceptable in production:

- Authentication and role-based access control via Keycloak (SSO)
- Secured endpoints by role (organizer, spectator)
- First real cloud deployment via Azure DevOps + Terraform Azure

The addition of the public catalogue and reservations is the first
audience-facing feature — only possible once security is in place.

**Why Keycloak**:
SSO centralized on-premise, accessible by all applications across
both zones (on-premise and Azure). Open source, zero cost,
enterprise-grade. Containerized (official Docker image) alongside
existing services in docker-compose.
Exposed publicly via Nginx reverse proxy (TLS, rate limiting,
minimal endpoint exposure) + native Keycloak brute force protection.

**Why SolidJS**:
Audience-facing application with unpredictable traffic peaks.
SolidJS compile-time reactivity delivers native performance
with minimal bundle size — the right tool for a high-read,
low-write public application.

**Why Azure Service Bus**:
Single outbound connection from on-premise (no inbound port required).
Managed, HA included. Abstracted behind IMessageBus interface —
broker can be swapped without touching business code.
Chosen over RabbitMQ to demonstrate cloud-native messaging
in a hybrid architecture.

**Why PostgreSQL**:
Polyglot persistence — SQL Server on-premise for events,
PostgreSQL on Azure for reservations and catalogue.
Azure Database for PostgreSQL is fully managed, elastic,
and cost-effective at this scale.

**Why Azure Front Door**:
HTTP cache layer in front of the public catalogue —
equivalent to Varnish on-premise. Geographically distributed,
zero ops, WAF included for Azure-side protection.

### V2.1 — Personalization

**Why this milestone**:
Reservations and catalogue are in place (V2.0).
User data exists. The next logical step is to leverage it —
recommend the right event to the right person at the right time.
Personalization is only possible once the data foundation is stable.

**Why hybrid recommendation (content-based + collaborative)**:
User preferences are contextual, not just categorical.
A spectator can enjoy jazz in winter and classical music in summer.
Content-based alone misses behavioral patterns.
Collaborative alone misses seasonal context.
Hybrid covers both.

**Why Python + FastAPI**:
Python is the standard for ML/data science ecosystems.
FastAPI is performant, async-native, and produces OpenAPI docs
out of the box — consistent with the .NET 8 APIs already in place.
Adding Python demonstrates polyglot backend capability,
a real-world pattern in data-heavy architectures.

**Why scikit-learn**:
Mature, well-documented, sufficient for collaborative filtering
and content-based recommendation at this scale.
No need for deep learning frameworks (PyTorch, TensorFlow)
at this volume — using the right tool for the right scale.

**Why pgvector**:
Vector similarity search directly in PostgreSQL — already provisioned.
No additional service required. Stores user preference embeddings
and enables semantic similarity queries.
Extension activated via Terraform configuration.

**Why CosmosDB MongoDB API**:
User preferences are semi-structured — flexible schema,
no complex relations. CosmosDB MongoDB API demonstrates
on-premise (MongoDB) to cloud (CosmosDB) portability :
same SDK, same code, different infrastructure.
Data stays within Azure private network —
user preferences never transit over public internet.

**Why Azure Container Apps**:
Personalization service runs alongside Reservation API
in the same Azure private network. Internal communication only —
preferences and recommendations never exposed to public internet.
Scale to zero when idle — cost optimized for unpredictable workloads.

### V2.2 — Professional artists

**Why this milestone**:
Catalogue and reservations are stable (V2.0).
Personalization is in place (V2.1).
Artists are the core content driver — without them,
events have no identity and recommendations have no depth.
Cancellation and rescheduling rules are already specified
and require a dedicated domain to enforce them.

**Why a dedicated Artist API**:
Database per service principle — Artist API owns its data.
No shared schema with EventsAPI. Independent deployment,
independent scaling, independent failure.
ArtistCancelled events published to Azure Service Bus
trigger downstream reactions (reservations, notifications)
without direct service-to-service coupling.

**Why SQL Server (dedicated instance)**:
Artist data is structured with complex relations —
artists, contracts, dates, event associations.
Relational model is the right fit.
Dedicated instance enforces microservice boundary —
EventsAPI cannot access Artist data directly.
Same technology as EventsAPI on-premise,
consistent operational model.

**Why Angular**:
Backoffice for producers — complex forms, stateful workflows
(artist creation, contract management, cancellation decisions).
Angular opinionated structure fits complex backoffice use cases.
NgRx manages cancellation workflow state explicitly —
a cancellation decision has multiple steps and side effects
that benefit from a predictable state machine.

**Why NgRx + RxJS**:
Cancellation and rescheduling workflows are asynchronous
and multi-step — organizer decision, notification dispatch,
reservation impact. RxJS models these event streams naturally.
NgRx makes state transitions explicit and auditable —
critical when financial decisions (refunds) are involved.

### V2.3 — Venue administration

**Why this milestone**:
Artists and cancellation rules are in place (V2.2).
Venue management closes the core domain triangle —
events need a venue, artists need a stage.
VenueUnavailable is a first-class business event
that triggers the same cancellation and rescheduling
rules already defined for artist cancellations.

**Why a dedicated Venue API**:
Database per service principle — Venue API owns its data.
Venues, availability calendars, and rental agreements
are a distinct bounded context from events and artists.
VenueUnavailable published to Azure Service Bus
triggers downstream reactions (organizer alert,
reservation impact) without direct service coupling.
Independent deployment and scaling.

**Why SQL Server (dedicated instance)**:
Venue data is structured with complex relations —
venues, rooms, capacity configurations, availability slots,
rental agreements. Relational model is the right fit.
Dedicated instance enforces microservice boundary.
Consistent operational model with EventsAPI and Artist API.

**Why React + Redux Toolkit**:
Venue administration is form-heavy with complex state —
availability calendar, room configurations, rental management.
Redux Toolkit manages this state predictably.
React Query handles server state (availability checks,
rental confirmations) with caching and synchronization.
Demonstrates React in a real backoffice context,
distinct from Angular (producer) and Vue.js (organizer).

**Future evolution**:
ReservationCreated consumption by Venue API is intentionally
deferred — the business need (fill rate per organizer
as a rental criterion) is identified but not yet validated.
Event-driven architecture makes this addition non-breaking —
a new consumer, no changes to existing producers.

---




### Additional frontends

| Frontend | Framework | Target users | Ecosystem to demonstrate |
|---|---|---|---|
| Event administration | Vue.js 3 (current) | Organizers | Pinia, Vue Router |
| Audience-facing | Angular | Spectators | NgRx, Angular Router, RxJS |
| Venue administration | React | Venue managers | Redux Toolkit / React Query, React Router |

### Functional features

- Reservation system — booking flow, capacity management, cancellation

### Security

- JWT authentication — `[Authorize]` on all endpoints, `[Authorize(Roles = "Admin")]` on `/admin/search/reindex`

### Architecture

- Event-driven architecture — message broker (RabbitMQ or Azure Service Bus)
- Microservices decomposition
- New functional features (TBD)
- ReindexAllAsync : current implementation match with the current target volume. Bulk calls to Elasticsearch would be consider to avoid single oversized bulk request at scale later
- Elasticsearch index mapping — currently created implicitly via ES dynamic mapping
  on first document write, no explicit mapping/analyzer defined in code. Needs an
  explicit mapping (controlled field types, French-text analyzer) before V1 to avoid
  an unversioned, order-dependent schema.
- Azure DevOps pipelines
- Terraform (Azure)
- Cloud