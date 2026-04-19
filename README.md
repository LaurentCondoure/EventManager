# Events Management

A cultural events management platform enabling organizers to publish events (concerts, shows, exhibitions) and users to discover them, book seats, and share their reviews.
API .NET built as part of a self-directed learning process started in February 2026 — focuced full-text search with Elasticsearch, distributed cache with Redis, infrastructure provisioned with Terraform.

---

## Tech Stack

**Backend** — .NET 8, ASP.NET Core, C#, Dapper, SQL Server, MongoDB, Redis, Elasticsearch  
**Frontend** — Vue.js 3, Pinia  
**Infrastructure** — Docker, Varnish, Terraform, Azure  
**DevOps** — Azure DevOps, xUnit, Serilog

---

## Repository Structure

```
EventManager/
├── backend/
│   ├── EventManager.Domain/
│   ├── EventManager.Infrastructure/
│   └── EventManager.Api/
├── frontend/
├── terraform/
├── documents/
│   ├── adr/
│   ├── specifications/
│   └── technical/
├── azure-pipelines.yml
└── README.md
```

> See [ADR-001](docs/adr/ADR-001-repository-structure.md) — mono-repository decision and component-scoped pipelines.

---

## Getting Started

_To be completed_

---

## API Endpoints

_To be completed — Swagger documentation available at `/swagger` after startup_

---

## Tests

_To be completed_

---

## Documentation

| Document | Description |
|----------|-------------|
| [ADR Index](documents/adr/00-index.md) | Architecture decision records |
| [Specifications](documents/Specifications/) | project definition, User stories, acceptance criteria and business rules |
| Architecture | _To be completed_ |
| Deployment | _To be completed_ |
| AI Usage | _To be completed_ |
