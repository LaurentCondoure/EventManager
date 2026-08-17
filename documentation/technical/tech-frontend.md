# Frontend — Concepts et Implémentation

**Author:** Laurent Condoure
**Date:** 2026-05-21  
**Status:** Accepted
**Project:** EventManager — Cultural Events Management Application  
**Objective:** Describes the frontend implementation

## Overview

The frontend is a Vue 3 single-page application built with Vite. It communicates with the ASP.NET Core API via a dev proxy and covers the full MVP feature set: event listing, search, event creation, and comments.

```
EventManagement.UI/
├── index.html
├── vite.config.js
├── src/
│   ├── main.js
│   ├── App.vue
│   ├── assets/main.css
│   ├── composables/useFormatters.js
│   ├── services/apiService.js
│   ├── stores/eventStore.js
│   ├── router/index.js
│   ├── components/
│   │   ├── EventCard.vue
│   │   └── EventSearch.vue
│   └── views/
│       ├── HomeView.vue
│       ├── SearchView.vue
│       ├── EventDetailView.vue
│       └── EventFormView.vue
```

---

## 1. Setup — Vite + Dependencies + Proxy

### Vite

Vite is the build tool. It serves the app in development with native ES modules (no bundling during dev) and produces an optimized build for production.

```javascript
// vite.config.js
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) }
  },
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5256', changeOrigin: true }
    }
  },
  test: { environment: 'jsdom', globals: true }
})
```

### API Proxy

The proxy forwards every request starting with `/api` from the Vite dev server (`localhost:5173`) to the API (`localhost:5256`). This means:

- No CORS configuration needed in development
- The frontend uses relative paths (`/api/events`) — no hardcoded API host
- The same relative paths work in production behind a reverse proxy (IIS, Nginx)

### CORS (production)

In development the Vite proxy eliminates cross-origin requests entirely — the browser sees a single origin (`localhost:5173`) for both the page and the API calls. No CORS headers are involved.

In production (IIS), the frontend is served from `localhost:8080` and the API from `localhost:5256` — two distinct origins. The browser enforces the *Same-Origin Policy*: it blocks responses from a different origin unless the server explicitly permits it via CORS headers.

The API declares an allowed-origins policy configured from `appsettings.json`:

```json
"Cors": {
  "AllowedOrigins": [ "http://localhost:5173", "http://localhost:8080" ]
}
```

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

app.UseCors("Frontend");
```

`WithOrigins(origins)` — only the listed origins are allowed, never `*`. Each environment (`appsettings.Development.json`, `appsettings.Production.json`) declares its own list without touching the code.

The frontend uses relative paths (`/api/events`) in both environments — the base URL never appears in the Vue source. In development the Vite proxy resolves it; in production IIS or Varnish resolves it.

### Dependencies

| Package | Role |
|---|---|
| `vue` | UI framework |
| `vue-router` | Client-side routing |
| `pinia` | State management |
| `chart.js` + `vue-chartjs` | Data visualization (future use) |
| `vite` | Build tool |
| `vitest` + `@vue/test-utils` | Unit testing |
| `eslint` + `eslint-plugin-vue` | Code quality |

---

## 2. API Service + Pinia Store + Router

### API Service (`services/apiService.js`)

A single `request()` helper centralizes all HTTP concerns: base URL, JSON headers, error parsing, and 204 handling.

```javascript
const API_BASE = '/api'

async function request(url, options = {}) {
  const response = await fetch(`${API_BASE}${url}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: response.statusText }))
    throw new Error(error.detail ?? error.message ?? `HTTP ${response.status}`)
  }

  if (response.status === 204) return null
  return response.json()
}
```

Error parsing tries `error.detail` first (ASP.NET Core `ProblemDetails` format), then `error.message`, then falls back to the HTTP status text.

Two service objects are exported:

```javascript
export const eventService = {
  getAll:        (page = 1, pageSize = 20) => request(`/events?page=${page}&pageSize=${pageSize}`),
  getById:       (id)                      => request(`/events/${id}`),
  getFull:       (id)                      => request(`/events/${id}/full`),
  search:        (q, page = 1)             => request(`/events/search?q=${encodeURIComponent(q)}&page=${page}`),
  create:        (data)                    => request('/events', { method: 'POST', body: JSON.stringify(data) }),
  update:        (id, data)                => request(`/events/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete:        (id)                      => request(`/events/${id}`, { method: 'DELETE' }),
  getCategories: ()                        => request('/events/categories'),
}

export const commentService = {
  getByEvent: (eventId)       => request(`/events/${eventId}/comments`),
  create:     (eventId, data) => request(`/events/${eventId}/comments`, { method: 'POST', body: JSON.stringify(data) })
}
```

`encodeURIComponent` on the search query prevents special characters from breaking the URL.

### Pinia Store (`stores/eventStore.js`)

The store manages the event list state shared across views — `HomeView` for the paginated list, `EventDetailView` for deletion (so the in-memory list stays in sync without a full refetch).

```javascript
export const useEventStore = defineStore('event', () => {
  const events      = ref([])
  const loading     = ref(false)
  const error       = ref(null)
  const currentPage = ref(1)
  const hasMore     = ref(true)

  async function fetchEvents(page = 1, pageSize = 20) {
    loading.value = true
    error.value   = null
    try {
      const data = await eventService.getAll(page, pageSize)
      events.value      = page === 1 ? data : [...events.value, ...data]  // append on load more
      currentPage.value = page
      hasMore.value     = data.length === pageSize                        // no more if partial page
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function createEvent(data) {
    const created = await eventService.create(data)
    events.value = [created, ...events.value]
    return created
  }

  async function updateEvent(id, data) {
    const updated = await eventService.update(id, data)
    events.value = events.value.map(e => e.id === id ? updated : e)
    return updated
  }

  async function deleteEvent(id) {
    await eventService.delete(id)
    events.value = events.value.filter(e => e.id !== id)
  }

  function loadMore() {
    if (!loading.value && hasMore.value)
      fetchEvents(currentPage.value + 1)
  }

  return { events, loading, error, hasMore, fetchEvents, createEvent, updateEvent, deleteEvent, loadMore }
})
```

`currentPage.value = page` is what makes `loadMore()` progress — without it, every call would re-request the same page. `hasMore` is derived from whether the API returned a full page: if `pageSize = 20` and only 12 events came back, there are no more pages. This is a manually-triggered "Load More" button (see `HomeView` below), not scroll-triggered infinite scroll — there is no scroll listener or `IntersectionObserver`.

`createEvent`, `updateEvent`, and `deleteEvent` all mutate `events.value` locally after a successful API call, so every view sharing the store stays in sync without an extra round-trip.

`EventDetailView` fetches its own event data directly via `eventService.getFull()` (detail data is not shared with other views), but goes through the store for `deleteEvent` — so a deletion is reflected immediately if the user navigates back to `HomeView`.

### Router (`router/index.js`)

```javascript
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/',                  component: HomeView },
    { path: '/events/:id',        component: () => import('@/views/EventDetailView.vue') },
    { path: '/events/:id/edit',   component: () => import('@/views/EventFormView.vue') },
    { path: '/create',            component: () => import('@/views/EventFormView.vue') },
    { path: '/search',            component: () => import('@/views/SearchView.vue') },
  ]
})
```

`HomeView` is imported statically (loaded on initial render). All other routes use dynamic imports — they are code-split into separate chunks and loaded on demand.

---

## 3. App.vue + Global Styles

### App.vue

Root component. Renders the navigation header and the `<RouterView />` outlet. No business logic.

```vue
<template>
  <header class="header">
    <nav>
      <RouterLink to="/">Événements</RouterLink>
      <RouterLink to="/create">Créer</RouterLink>
      <RouterLink to="/search">Recherche</RouterLink>
    </nav>
  </header>
  <main class="main">
    <RouterView />
  </main>
</template>
```

`router-link-active` is styled in CSS to highlight the current route automatically — no JavaScript required.

### Global Styles (`assets/main.css`)

Key layout decisions:

| Rule | Value | Reason |
|---|---|---|
| `max-width` on `.main` | 1200px | Prevents excessive line length on wide screens |
| `.events-grid` | `auto-fill, minmax(280px, 1fr)` | Responsive grid without media queries |
| `.card` | `display: block` on `RouterLink` | Makes the entire card clickable as a link |
| `.stars` | `color: #f5a623` | Visual rating display in comments |

---

## 4. EventCard + EventSearch Components

### EventCard (`components/EventCard.vue`)

Reusable card displayed in the event grid. Accepts a single `event` prop.

```vue
<script setup>
defineProps({ event: { type: Object, required: true } })
const { formatDate, formatPrice } = useFormatters()
</script>
```

The entire card is wrapped in a `<RouterLink>` — clicking anywhere on the card navigates to the event detail page.

`artistName` is rendered conditionally — it is an optional field on the domain model:
```vue
<p v-if="event.artistName" class="card-artist">{{ event.artistName }}</p>
```

### EventSearch (`components/EventSearch.vue`)

Self-contained search component: owns its own `query`, `results`, `loading`, and `searched` state. Does not use the Pinia store — search results are transient and not shared.

```javascript
async function search() {
  if (!query.value.trim()) return   // guard: no empty queries
  loading.value  = true
  searched.value = false
  try {
    results.value  = await eventService.search(query.value)
    searched.value = true
  } finally {
    loading.value = false
  }
}
```

`searched` tracks whether a search has been submitted, to distinguish "no results" from "not searched yet" in the template:

```vue
<div v-else-if="searched" class="empty">Aucun résultat pour "{{ query }}"</div>
```

Enter key triggers search via `@keyup.enter="search"` — no form submission required.

---

## 5. HomeView + SearchView

### HomeView (`views/HomeView.vue`)

Fetches the event list from the Pinia store on mount. Manages three display states:

```vue
<div v-if="store.loading && store.events.length === 0">Chargement...</div>
<div v-else-if="store.error">{{ store.error }}</div>
<div v-else-if="store.events.length === 0">Aucun événement disponible.</div>
<div v-else class="events-grid">...</div>
```

The loading guard (`store.loading && store.events.length === 0`) ensures the spinner only shows on initial load — not on "load more", where existing events remain visible.

The "Load More" button is shown only when `store.hasMore` is true:

```vue
<div v-if="store.hasMore" class="load-more">
  <button @click="store.loadMore" :disabled="store.loading">
    {{ store.loading ? 'Chargement...' : 'Charger plus' }}
  </button>
</div>
```

### SearchView (`views/SearchView.vue`)

Thin wrapper — delegates entirely to `EventSearch`. Exists to provide a routable page with a title.

```vue
<template>
  <div>
    <h1>Recherche</h1>
    <EventSearch />
  </div>
</template>
```

---

## 6. EventDetailView

Fetches the full event (event data + comments) from `GET /api/events/{id}/full` on mount. Manages both the event display and the comment submission form.

### Data fetch

```javascript
onMounted(async () => {
  loading.value = true
  try {
    data.value = await eventService.getFull(route.params.id)
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
})
```

`data.value` holds `{ event: {...}, comments: [...] }` — the shape returned by `GET /api/events/{id}/full`.

### Delete

```javascript
async function confirmDelete() {
  if (!confirm(`Supprimer définitivement "${data.value.event.title}" ? Cette action est irréversible.`))
    return

  deleting.value    = true
  deleteError.value = null
  try {
    await store.deleteEvent(route.params.id)
    router.push('/')
  } catch (e) {
    deleteError.value = e.message
  } finally {
    deleting.value = false
  }
}
```

The native `confirm()` dialog blocks the destructive action until the user explicitly accepts — no custom modal component needed for a single yes/no confirmation. The call goes through `store.deleteEvent`, not a direct `eventService` call, so the event list held by the store stays consistent if the user navigates back to `HomeView` after deleting.

### Comment submission

```javascript
async function submitComment() {
  submitting.value = true
  try {
    await commentService.create(route.params.id, form.value)
    data.value = await eventService.getFull(route.params.id)  // reload to get new comment with id + createdAt
    form.value = { userName: '', rating: 5, text: '' }
  } finally {
    submitting.value = false
  }
}
```

After posting, `getFull` is called again to reload the full event with the newly created comment. This is deliberate: the POST response does not include the full comment list, and the reloaded data ensures `id` and `createdAt` (set server-side) are present. A local push into `data.value.comments` would be more efficient but would require the POST to return the created comment with its server-assigned fields. This is a known trade-off, to be revisited.

### Star rating display

```vue
<span class="stars">{{ '★'.repeat(c.rating) }}{{ '☆'.repeat(5 - c.rating) }}</span>
```

Filled and empty star characters repeat based on the rating integer (1–5). No library needed.

---

## 7. EventFormView

Handles both event **creation** (`/create`) and **editing** (`/events/:id/edit`) — a single component that adapts its behaviour based on the route.

### Smart form — create vs edit detection

```javascript
const isEditMode = computed(() => !!route.params.id)
```

`route.params.id` is present on `/events/:id/edit` and absent on `/create`. This single computed drives the entire view: title, button label, back link, API call, and redirect target.

```vue
<h1>{{ isEditMode ? 'Modifier l\'événement' : 'Créer un événement' }}</h1>
```

This pattern demonstrates dynamic view construction from functional requirements — one component, two use cases, zero duplication.

### Category fetch

```javascript
const categories = ref([])
const loading    = ref(false)
const loadError  = ref(null)

onMounted(async () => {
  loading.value = true
  try {
    categories.value = await eventService.getCategories()

    if (isEditMode.value) {
      const event = await eventService.getById(route.params.id)
      form.value = {
        title:       event.title,
        description: event.description,
        date:        new Date(event.date).toISOString().slice(0, 16),  // ISO 8601 → datetime-local
        location:    event.location,
        category:    event.category,
        artistName:  event.artistName ?? '',                            // null → empty string
        capacity:    event.capacity,
        price:       event.price
      }
    }
  } catch (e) {
    loadError.value = isEditMode.value
      ? 'Impossible de charger l\'événement.'
      : 'Impossible de charger les catégories. Veuillez recharger la page.'
  } finally {
    loading.value = false
  }
})
```

In edit mode, the existing event is fetched and pre-fills the form. Each field is copied explicitly rather than spread (`{ ...event, ... }`) — the API response also carries `id`, `createdAt`, and `updatedAt`, which must never end up in the payload sent back on submit. Two fields are transformed in reverse compared to submission:

- `date` is sliced to `YYYY-MM-DDTHH:mm` — the format required by `<input type="datetime-local">`
- `artistName` null is converted to an empty string — HTML inputs cannot hold `null`

The `try/catch` distinguishes the two things that can fail on mount: if categories fail to load, the form is unusable regardless of mode; if the event itself fails to load (edit mode only), the error message is specific to that case.

The dropdown is disabled while categories are loading:

```vue
<select v-model="form.category" required :disabled="categories.length === 0">
  <option value="" disabled>{{ categories.length === 0 ? 'Chargement...' : 'Sélectionner' }}</option>
  <option v-for="cat in categories" :key="cat" :value="cat">{{ cat }}</option>
</select>
```

### Payload transformation

Before sending to the API, two fields are transformed:

```javascript
const payload = {
  ...form.value,
  date:       new Date(form.value.date).toISOString(),  // datetime-local → ISO 8601 UTC
  artistName: form.value.artistName || null              // empty string → null
}
```

`datetime-local` inputs produce a local datetime string (`2026-05-01T20:00`). The API expects ISO 8601 UTC (`2026-05-01T18:00:00.000Z`). `new Date(...).toISOString()` handles the conversion including timezone offset.

`artistName` is optional in the domain model. An empty string from the form is converted to `null` so the backend receives the correct value instead of failing validation on an empty non-null string.

### Submit — create or update

```javascript
if (isEditMode.value) {
  await store.updateEvent(route.params.id, payload)
  router.push(`/events/${route.params.id}`)
} else {
  const created = await store.createEvent(payload)
  router.push(`/events/${created.id}`)
}
```

- **Create** — redirects to the newly created event's detail page (id comes from the API response)
- **Edit** — redirects back to the same event's detail page (id is already known from the route)

Both paths go through the Pinia store, keeping the in-memory event list in sync without a full refetch.

---

## 8. Composable useFormatters

`formatDate` and `formatPrice` were duplicated in `EventCard` and `EventDetailView`. They are extracted into a composable.

```javascript
// composables/useFormatters.js
export function useFormatters() {
  function formatDate(date) {
    return new Date(date).toLocaleDateString('fr-FR', {
      day: 'numeric', month: 'long', year: 'numeric'
    })
  }

  function formatPrice(price) {
    return price === 0 ? 'Gratuit' : `${price} €`
  }

  return { formatDate, formatPrice }
}
```

Usage in any component:

```javascript
const { formatDate, formatPrice } = useFormatters()
```

### Why a composable and not a utility module

A plain utility module (`utils/formatters.js`) would also work. A composable is chosen for consistency with the Vue 3 pattern — it fits naturally in `<script setup>` and would support reactive arguments in the future (e.g., locale switching) without changing the call site.
