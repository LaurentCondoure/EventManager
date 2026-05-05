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
  getByEvent: (eventId)        => request(`/events/${eventId}/comments`),
  create:     (eventId, data)  => request(`/events/${eventId}/comments`, { method: 'POST', body: JSON.stringify(data) })
}
