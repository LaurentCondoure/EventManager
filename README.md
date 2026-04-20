# Events Management

A cultural events management platform enabling organizers to publish events (concerts, shows, exhibitions) and users to discover them, book seats, and share their reviews.

Technical demonstration project built as part of an intensive 52-hour training program.

---

## Tech Stack

**Backend** — .NET 8, ASP.NET Core, C#, Dapper, SQL Server, MongoDB, Redis, Elasticsearch  
**Frontend** — Vue.js 3, Pinia  
**Infrastructure** — Docker, Varnish, Terraform, Azure  
**DevOps** — Azure DevOps, xUnit, Serilog

---

## Repository Structure

```
events-management/
├── backend/
│   ├── EventsAPI.Domain/
│   ├── EventsAPI.Infrastructure/
│   └── EventsAPI.Api/
├── frontend/
├── terraform/
├── docs/
│   ├── adr/
│   ├── functionnal/
│   └── technical/
├── azure-pipelines.yml
└── README.md
```

> See [ADR-001](documents/adr/ADR-001-repository-structure.md) — mono-repository decision and component-scoped pipelines.

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
| [Specifications](documents/functionnal/) | Project definition, user stories, acceptance criteria and business rules |
| [Data Model](documents/technical/DATA_MODEL.md) | Database schema (SQL Server, MongoDB) and key design decisions |
| [Technical Design](documents/technical/TECHNICAL_DESIGN.md) | Target architecture, data flows, technical choices, API endpoints |
| Architecture | _To be completed_ |
| Deployment | _To be completed_ |
| AI Usage | _To be completed_ |
