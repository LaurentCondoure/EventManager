# Vue.js 3 — Concepts et Implémentation

**Author:** Laurent Condoure
**Date:** 2026-05-22  
**Status:** Accepted
**Project:** EventManager — Cultural Events Management Application  
**Objective:** Introduces core Vue.js 3 concepts illustrated with examples from this project

## What is Vue.js 3

Vue.js 3 is a progressive JavaScript framework for building user interfaces. It is component-based: the UI is broken into self-contained pieces (components) that encapsulate their own template, logic, and styles.

Vue 3 introduced the **Composition API** alongside the existing Options API. The Composition API organises code by logical concern rather than by option type (`data`, `methods`, `computed`), making components easier to read and extract into reusable composables.

---

## Composition API — `<script setup>`

`<script setup>` is the recommended syntax for Vue 3 components. Code written inside is executed once per component instantiation and its top-level bindings (variables, functions) are automatically available in the template.

```vue
<script setup>
import { ref, onMounted } from 'vue'

const count = ref(0)               // reactive variable
function increment() { count.value++ }
</script>

<template>
  <button @click="increment">{{ count }}</button>
</template>
```

### Options API vs Composition API

| | Options API | Composition API |
|---|---|---|
| Code organisation | By option (`data`, `methods`) | By logical concern |
| Reuse | Mixins (implicit, conflict-prone) | Composables (explicit imports) |
| TypeScript | Possible but awkward | Natural |
| Boilerplate | More | Less |

---

## Reactivity — `ref`

`ref()` wraps a value in a reactive container. The value is accessed via `.value` in JavaScript and directly in the template.

```javascript
const events   = ref([])          // reactive array
const loading  = ref(false)       // reactive boolean
const error    = ref(null)        // reactive nullable

events.value = [{ id: '1' }]     // triggers re-render
```

### `v-model` — two-way binding

`v-model` binds a form input to a ref. Changes in the input update the ref; changes in the ref update the input.

```vue
const query = ref('')

<input v-model="query" />             <!-- string -->
<input v-model.number="form.price" /> <!-- cast to number -->
```

`.number` modifier casts the input value to a number automatically — important for `type="number"` inputs where the DOM always returns a string.

---

## Lifecycle — `onMounted`

`onMounted` runs after the component is inserted in the DOM. Used for data fetching and any operation that requires the DOM to exist.

```javascript
onMounted(async () => {
  try {
    events.value = await eventService.getAll()
  } catch (e) {
    error.value = e.message
  }
})
```

**Why fetch in `onMounted` and not at the top level?**  
Top-level `await` in `<script setup>` suspends the entire component until the promise resolves — the component does not render at all during the wait. `onMounted` renders the component immediately (with a loading state) then fetches — a better user experience.

---

## Template Directives

### Conditional rendering

```vue
<div v-if="loading">Chargement...</div>
<div v-else-if="error">{{ error }}</div>
<div v-else>Content</div>
```

`v-if` / `v-else-if` / `v-else` form a chain — only one block renders. The non-rendering blocks are removed from the DOM entirely (not just hidden).

### List rendering

```vue
<EventCard
  v-for="event in events"
  :key="event.id"
  :event="event"
/>
```

`:key` is required — Vue uses it to track which DOM nodes correspond to which list items during re-renders. Without it, Vue may reuse incorrect nodes when the list changes.

### Event handling

```vue
<button @click="search">Rechercher</button>
<input @keyup.enter="search" />   <!-- key modifier -->
```

`@` is shorthand for `v-on:`. Key modifiers (`.enter`, `.esc`) filter events by key code.

---

## Props — `defineProps`

Props are the mechanism for passing data from a parent to a child component.

```javascript
// EventCard.vue
defineProps({
  event: { type: Object, required: true }
})
```

Props are read-only in the child — the child must not mutate them. If a child needs to communicate back to its parent, it emits an event.

---

## Components

### Reusable components

Components encapsulate a piece of UI that is used in multiple places. In this project:

| Component | Reused in |
|---|---|
| `EventCard` | `HomeView`, `EventSearch` |
| `EventSearch` | `SearchView` |

### Stubbing in tests

Components that have external dependencies (router, API) are stubbed in tests to isolate the component under test:

```javascript
// Replace RouterLink with a plain <a> in tests
const routerLinkStub = { template: '<a :href="to"><slot /></a>', props: ['to'] }

mount(EventCard, {
  global: { stubs: { RouterLink: routerLinkStub } }
})
```

---

## Vue Router

### Route declaration

```javascript
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/',           component: HomeView },                            // static import
    { path: '/events/:id', component: () => import('@/views/EventDetailView.vue') }, // lazy
    { path: '/create',     component: () => import('@/views/EventFormView.vue') },
    { path: '/search',     component: () => import('@/views/SearchView.vue') },
  ]
})
```

**Static vs lazy imports:**  
`HomeView` is statically imported — it is always needed on first load. Other routes use dynamic `import()` — Vite splits them into separate chunks loaded only when the route is visited.

### In-component usage

```javascript
const route  = useRoute()   // read current route (params, query)
const router = useRouter()  // navigate programmatically

route.params.id             // /events/:id → the id segment
router.push('/events/123')  // navigate after form submission
```

### RouterLink

`<RouterLink to="/create">` renders an `<a>` tag and applies the `router-link-active` CSS class automatically when the target route is active. No manual class management needed.

---

## Pinia — State Management

Pinia is the official Vue 3 state management library. It replaces Vuex with a simpler API based on the Composition API.

### Store definition

```javascript
export const useEventStore = defineStore('event', () => {
  // state
  const events  = ref([])
  const loading = ref(false)
  const error   = ref(null)

  // actions
  async function fetchEvents(page = 1) {
    loading.value = true
    error.value   = null           // clear any previous failure before retrying
    try {
      const data    = await eventService.getAll(page)
      events.value  = page === 1 ? data : [...events.value, ...data]
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  return { events, loading, error, fetchEvents }
})
```

### When to use the store vs local state

| | Pinia store | Local `ref` |
|---|---|---|
| **Shared across views** | ✅ | ❌ |
| **Persists across navigation** | ✅ | ❌ (reset on unmount) |
| **Used in one component only** | Overkill | ✅ |

In this project, the event list (`HomeView`) uses the store because the pagination state must survive navigation (back button). Search results and event detail use local state — they are transient and not shared.

---

## Composables

A composable is a function that uses the Composition API to encapsulate and reuse stateful or pure logic across components.

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

**Composable vs utility module:**  
A plain utility module (`utils/formatters.js`) would also work for pure functions. The composable pattern is chosen for consistency with the Vue 3 ecosystem — it fits naturally in `<script setup>` and supports reactive arguments in the future (e.g., locale switching) without changing the call site.

### Convention

Composable names start with `use` by convention (`useFormatters`, `useRoute`, `useEventStore`). This signals to the reader that the function may use reactive state or lifecycle hooks.

---

## ESLint Configuration

ESLint requires a configuration file at the project root. Without it, ESLint cannot determine which rules to apply and exits with an error — even if the package is installed and the `lint` script is declared in `package.json`.

### Configuration file

```json
// .eslintrc.json
{
  "env": {
    "browser": true,
    "es2022": true
  },
  "extends": [
    "eslint:recommended",
    "plugin:vue/vue3-recommended"
  ],
  "parserOptions": {
    "ecmaVersion": "latest",
    "sourceType": "module"
  },
  "rules": {
    "vue/multi-word-component-names": "off"
  }
}
```

`plugin:vue/vue3-recommended` includes `vue-eslint-parser` (the parser for `.vue` files) and enables Vue 3 specific rules. `multi-word-component-names` is disabled — the rule requires component names to be at least two words (e.g. `AppHeader`), which conflicts with root-level components like `App.vue`.

### Lint script scope

The `lint` script must target explicit directories rather than `.` (project root):

```json
"lint": "eslint src tests --ext .js,.vue"
```

Using `.` causes ESLint to walk the entire project tree. While `node_modules` is ignored by default, other generated or config files outside `src/` may trigger unexpected errors. Targeting `src` and `tests` explicitly keeps the scope predictable and the CI output clean.

---

## Testing with Vitest + Vue Test Utils

### Setup

```javascript
// vite.config.js
test: {
  environment: 'jsdom',  // browser-like DOM in Node
  globals:     true      // describe, it, expect available without imports
}
```

### Mounting a component

```javascript
import { mount } from '@vue/test-utils'

const wrapper = mount(MyComponent, {
  props:  { event: { id: '1', title: 'Jazz' } },
  global: { stubs: { RouterLink: routerLinkStub } }
})
```

### Async interactions

```javascript
import { flushPromises } from '@vue/test-utils'

await wrapper.find('button').trigger('click')
await flushPromises()  // wait for all pending promises (fetch, store actions)

expect(wrapper.text()).toContain('Result')
```

`flushPromises` is essential when testing code that involves `onMounted` data fetching or user interactions that trigger async actions. Without it, assertions run before the promises resolve.

---

## Error Handling — Services throw, Views catch

In the backend, a global exception handler middleware intercepts unhandled exceptions and returns a structured error response — controllers do not need try/catch blocks.

In the frontend, there is no equivalent global handler. Each view is responsible for catching errors from the service layer and deciding how to present them.

```javascript
// apiService.js — throws, never catches
async function request(url, options = {}) {
  const response = await fetch(`${API_BASE}${url}`, { ... })
  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: response.statusText }))
    throw new Error(error.detail ?? error.message ?? `HTTP ${response.status}`)
  }
  return response.json()
}
```

```javascript
// EventFormView.vue — catches, decides what to show
onMounted(async () => {
  try {
    categories.value = await eventService.getCategories()
  } catch {
    error.value = 'Impossible de charger les catégories. Veuillez recharger la page.'
  }
})
```

**Why not catch in the service?**  
A service that swallows its errors forces the caller to guess whether an empty result means "no data" or "request failed". The view has the UI context to decide whether an error should show a message, retry silently, or redirect. The service does not — it only handles transport.

| Layer | Backend | Frontend |
|---|---|---|
| **Transport** | Controller → throws domain exceptions | `apiService` → throws on `!response.ok` |
| **Error handling** | Global middleware catches, returns 4xx/5xx | View catches, updates `error` ref |
| **Caller** | No try/catch needed in controllers | try/catch required in each view |

---

### Mocking modules

```javascript
vi.mock('@/services/apiService', () => ({
  eventService: { search: vi.fn() }
}))
```

`vi.mock` is hoisted to the top of the file by Vitest — it intercepts the module before any imports resolve. This means the component under test always receives the mocked version.

### Testing Pinia stores

```javascript
import { setActivePinia, createPinia } from 'pinia'

beforeEach(() => setActivePinia(createPinia()))  // fresh store for each test
```

For component tests, pass `createPinia()` in `global.plugins` instead:

```javascript
mount(MyComponent, {
  global: { plugins: [createPinia()] }
})
```

### Coverage

Coverage requires `@vitest/coverage-v8` (separate package from Vitest) and a `coverage` block in `vite.config.js`:

```javascript
// vite.config.js
test: {
  environment: 'jsdom',
  globals: true,
  coverage: {
    provider: 'v8',
    reporter: ['lcov', 'text'],                          // lcov for CI, text for local terminal output
    reportsDirectory: './coverage',
    include: ['src/**/*.{js,vue}'],
    exclude: ['src/main.js', 'src/router/**', 'src/App.vue']
  }
}
```

Run with coverage:

```bash
npm run test:unit -- --run --coverage
```

**Why `lcov` and `text` together?**  
`lcov` produces a `lcov.info` file that Codecov and most CI coverage tools parse natively — it is not human-readable on its own. `text` prints a per-file summary straight to the terminal, useful when running coverage locally. Both are kept; `html` is not, since no one opens a browser report in this workflow.

`include`/`exclude` scope coverage to actual application code: `main.js` (bootstrap), the router table, and `App.vue` (a thin shell with no logic) would only dilute the percentage without indicating anything about test quality.

**Codecov flags**  
When a project has multiple coverage sources (backend + frontend), Codecov `flags` distinguish them:

```yaml
# CI — frontend upload
- uses: codecov/codecov-action@v4
  with:
    directory: frontend/EventManagement.UI/coverage
    flags: frontend

# CI — backend upload
- uses: codecov/codecov-action@v4
  with:
    directory: ./coverage
    flags: backend
```

Each flag appears as a separate view on Codecov; the combined total reflects both sources.
