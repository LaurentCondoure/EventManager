# Roadmap

## In progress

| Item | Status |
|---|---|
| PUT/DELETE endpoints + Redis cache invalidation | Pending |
| ADR-011 — Varnish + Redis cache strategy | Pending |
| IIS deployment | Pending |
| Azure DevOps pipelines | Pending |
| Terraform (local + Azure) | Pending |

---

## Planned evolutions

### Additional frontends

| Frontend | Framework | Target users | Ecosystem to demonstrate |
|---|---|---|---|
| Event administration | Vue.js 3 (current) | Organizers | Pinia, Vue Router |
| Audience-facing | Angular | Spectators | NgRx, Angular Router, RxJS |
| Venue administration | React | Venue managers | Redux Toolkit / React Query, React Router |

### Architecture

- Event-driven architecture — message broker (RabbitMQ or Azure Service Bus)
- Microservices decomposition
- New functional features (TBD)
