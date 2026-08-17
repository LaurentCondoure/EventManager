# API Reference — Events

**Reference:** API-V0-001
**Status:** Validated
**Version:** V0 — POC
**Date:** 2026-04-19

> This document describes the API contracts for the Event Management domain.
> For technical flows and architecture diagrams, see [design-v0-event-flows.md](../architecture/design/design-event-flows.md).
> For technology choices, see the ADRs referenced in [dat-eventmanager.md](../architecture/tad-eventmanager.md).

---

## Base URL

| Environment | URL |
|---|---|
| Local (direct) | `http://localhost:5000` |
| Local (via Varnish) | `http://localhost:8080` |

---

## Endpoints

### GET /api/events

Retrieve a paginated list of upcoming events.

**Query parameters:**

| Parameter | Type | Required | Default | Constraints |
|---|---|---|---|---|
| `page` | int | No | 1 | — |
| `pageSize` | int | No | 20 | max 50 |

**Cache headers:** `Cache-Control: public, max-age=300`

**Response 200 OK:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Concert Jazz au Sunset",
    "description": "Soirée jazz avec quartet exceptionnel...",
    "date": "2026-05-15T20:00:00Z",
    "location": "Sunset Jazz Club, Paris",
    "capacity": 150,
    "price": 25.00,
    "category": "Concert",
    "artistName": "Miles Quartet",
    "createdAt": "2026-04-15T10:30:00Z",
    "updatedAt": null
  }
]
```

---

### GET /api/events/{id}

Retrieve the details of a single event.

**Path parameters:**

| Parameter | Type | Required |
|---|---|---|
| `id` | GUID | Yes |

**Response 200 OK:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Concert Jazz au Sunset",
  "description": "Soirée jazz avec quartet exceptionnel...",
  "date": "2026-05-15T20:00:00Z",
  "location": "Sunset Jazz Club, Paris",
  "capacity": 150,
  "price": 25.00,
  "category": "Concert",
  "artistName": "Miles Quartet",
  "createdAt": "2026-04-15T10:30:00Z",
  "updatedAt": null
}
```

**Response 404 Not Found:**
```json
{ "error": "Event not found" }
```

**Response 500 Internal Server Error:**
```json
{ "status": 500, "message": "An unexpected error occurred.", "requestId": "<guid>" }
```

---

### POST /api/events

Create a new event.

**Request body:**
```json
{
  "title": "Concert Jazz au Sunset",
  "description": "Soirée jazz avec quartet exceptionnel...",
  "date": "2026-05-15T20:00:00Z",
  "location": "Sunset Jazz Club, Paris",
  "capacity": 150,
  "price": 25.00,
  "category": "Concert",
  "artistName": "Miles Quartet"
}
```

**Response 201 Created:**

Full event object. Header: `Location: /api/events/{id}`

**Response 400 Bad Request:**
```json
{
  "errors": {
    "Date": ["La date de l'événement doit être aujourd'hui ou dans le futur"],
    "Capacity": ["La capacité doit être supérieure à 0"]
  }
}
```

**Response 500 Internal Server Error:**
```json
{ "status": 500, "message": "An unexpected error occurred.", "requestId": "<guid>" }
```

---

### GET /api/events/search

Search events by keyword across title, description, category, and artist name.

**Query parameters:**

| Parameter | Type | Required | Default | Constraints |
|---|---|---|---|---|
| `q` | string | Yes | — | Non-empty |
| `page` | int | No | 1 | — |
| `pageSize` | int | No | 20 | max 50 |

**Response 200 OK:**

Array of event objects. Empty array if no results.

**Response 400 Bad Request:**

Missing or empty `q` parameter.

**Response 500 Internal Server Error:**
```json
{ "status": 500, "message": "An unexpected error occurred.", "requestId": "<guid>" }
```

---

### GET /api/events/stats/by-category

Retrieve event count grouped by category.

**Response 200 OK:**
```json
[
  { "category": "Concert", "count": 15 },
  { "category": "Théâtre", "count": 8 },
  { "category": "Exposition", "count": 12 },
  { "category": "Conférence", "count": 5 },
  { "category": "Spectacle", "count": 6 },
  { "category": "Autre", "count": 3 }
]
```

---

### POST /api/events/{eventId}/comments

Add a comment to an event.

**Path parameters:**

| Parameter | Type | Required |
|---|---|---|
| `eventId` | GUID | Yes |

**Request body:**
```json
{
  "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "userName": "Thomas Martin",
  "text": "Excellente soirée, ambiance chaleureuse !",
  "rating": 5
}
```

**Validation rules:**

| Field | Rule |
|---|---|
| `rating` | Between 1 and 5 |
| `text` | Max 1000 characters |

**Response 201 Created:**
```json
{
  "id": "507f1f77bcf86cd799439011",
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "userName": "Thomas Martin",
  "text": "Excellente soirée, ambiance chaleureuse !",
  "rating": 5,
  "createdAt": "2026-05-16T22:30:00Z"
}
```

**Response 400 Bad Request:**
```json
{
  "errors": {
    "Rating": ["La note doit être entre 1 et 5"],
    "Text": ["Le commentaire ne doit pas dépasser 1000 caractères"]
  }
}
```

**Response 404 Not Found:** Event does not exist.

**Response 500 Internal Server Error:**
```json
{ "status": 500, "message": "An unexpected error occurred.", "requestId": "<guid>" }
```

---

### GET /api/events/{eventId}/comments

Retrieve all comments for an event.

**Path parameters:**

| Parameter | Type | Required |
|---|---|---|
| `eventId` | GUID | Yes |

**Response 200 OK:**
```json
[
  {
    "id": "507f1f77bcf86cd799439011",
    "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "userName": "Thomas Martin",
    "text": "Excellente soirée, ambiance chaleureuse !",
    "rating": 5,
    "createdAt": "2026-05-16T22:30:00Z"
  }
]
```

Empty array if no comments: `[]`

**Response 404 Not Found:** Event does not exist.

**Response 500 Internal Server Error:**
```json
{ "status": 500, "message": "An unexpected error occurred.", "requestId": "<guid>" }
```

---

## Error Format — Standard

All 500 errors follow this structure:

```json
{
  "status": 500,
  "message": "An unexpected error occurred.",
  "requestId": "<guid>"
}
```

The `requestId` field is a unique identifier that allows tracing the error in application logs.

---

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | 2026-04-19 | Document created from design.md |
