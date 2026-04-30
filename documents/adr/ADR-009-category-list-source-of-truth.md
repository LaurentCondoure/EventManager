# ADR 009: Category List — Single Source of Truth

## Status
Accepted

## Context

Valid event categories are defined in two places:

- **Backend** — `ValidCategories` constant used by the FluentValidation validator (`CreateEventInputValidator`) to enforce the allowed values on `POST /api/events`.
- **Frontend** — `const categories = [...]` hardcoded in `EventFormView.vue`, used to populate the category dropdown.

Any change to the category list (adding "Festival", removing "Autre") requires two coordinated edits across two codebases. A mismatch — frontend offers a value the backend rejects — produces a silent `400 Bad Request` that is difficult to diagnose.

The backend is already the authoritative source: FluentValidation rejects any value not in `ValidCategories`. The frontend list is a derivative copy that must stay in sync manually.

## Alternatives Considered

### Alternative 1: Dedicated endpoint `GET /api/events/categories` (chosen)

The backend exposes the category list via a new endpoint. The frontend fetches it once on form mount and populates the dropdown dynamically.

```
GET /api/events/categories
→ ["Concert", "Théâtre", "Exposition", "Conférence", "Spectacle", "Autre"]
```

**Chosen because:**
- Eliminates duplication with minimal complexity — one endpoint, one store entry, one fetch.
- The frontend dropdown is guaranteed to match the validator: a value the backend rejects cannot appear in the UI.
- Natural fit for the existing Minimal API `MinimalApiEndpoints.cs` — a utility endpoint with no business logic and no need for FluentValidation or controller structure.
- Extensible: adding a category requires a single backend change; the frontend updates automatically.

### Alternative 2: Metadata endpoint `GET /api/config`

A single endpoint returns all shared configuration — categories, validation limits, feature flags. The frontend fetches it at application startup and stores the result in a dedicated Pinia store.

**Rejected for now:** introduces an abstraction (config store, config DTO) that is not yet justified by the current scope. One shared value does not warrant a general-purpose config contract. This becomes the right choice if multiple configuration values need to be shared across the boundary — at that point, the dedicated categories endpoint can be absorbed into a broader metadata endpoint.

## Decision

A new endpoint is added to `MinimalApiEndpoints.cs`:

```csharp
app.MapGet("/api/events/categories", () =>
    Results.Ok(CreateEventInputValidator.ValidCategories))
   .WithTags("Events")
   .WithSummary("Returns the list of valid event categories")
   .RequireRateLimiting("fixed");
```

`ValidCategories` is promoted to `public static` on `CreateEventInputValidator` so it can be referenced from the endpoint without duplication.

The frontend fetches the list in `EventFormView.vue` on mount and replaces the hardcoded array:

```javascript
const categories = ref([])
onMounted(async () => {
  categories.value = await request('/events/categories')
})
```

## Consequences

### Positive

- **Single source of truth** — `ValidCategories` in the validator drives both server-side rejection and client-side display. They cannot drift.
- **Zero maintenance** — adding or renaming a category requires one backend change; no frontend coordination needed.
- **Consistent with Minimal API usage** — utility endpoint with no business logic, consistent with the pattern established for `/health`.

### Negative

- **Extra HTTP request on form load** — one additional round-trip before the create form is interactive. At typical Redis/API latency this is negligible; a loading state on the dropdown handles the brief delay.
- **Form temporarily unusable if the request fails** — the dropdown is empty until the fetch completes. Mitigated by a fallback empty state and the existing error handling in the API service.

## Related Decisions

- ADR-003: Clean Architecture — utility endpoint placed at the API layer
