import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useEventStore } from '@/stores/eventStore'
import { eventService } from '@/services/apiService'

vi.mock('@/services/apiService', () => ({
  eventService: {
    getAll:  vi.fn(),
    create:  vi.fn(),
    update:  vi.fn(),
    delete:  vi.fn()
  }
}))

const fullPage  = Array.from({ length: 20 }, (_, i) => ({ id: String(i), title: `Event ${i}` }))
const partialPage = [{ id: '1', title: 'Event A' }]

describe('eventStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  // ── fetchEvents ───────────────────────────────────────────────────────────

  describe('fetchEvents', () => {
    it('sets events on first page', async () => {
      eventService.getAll.mockResolvedValue(partialPage)
      const store = useEventStore()
      await store.fetchEvents(1)
      expect(store.events).toEqual(partialPage)
    })

    it('appends events on subsequent pages', async () => {
      const page2 = [{ id: '2', title: 'Event B' }]
      eventService.getAll.mockResolvedValueOnce(partialPage).mockResolvedValueOnce(page2)
      const store = useEventStore()
      await store.fetchEvents(1)
      await store.fetchEvents(2)
      expect(store.events).toHaveLength(2)
      expect(store.events[1].id).toBe('2')
    })

    it('sets hasMore to true when page is full', async () => {
      eventService.getAll.mockResolvedValue(fullPage)
      const store = useEventStore()
      await store.fetchEvents(1)
      expect(store.hasMore).toBe(true)
    })

    it('sets hasMore to false when page is not full', async () => {
      eventService.getAll.mockResolvedValue(partialPage)
      const store = useEventStore()
      await store.fetchEvents(1)
      expect(store.hasMore).toBe(false)
    })

    it('sets error message on failure', async () => {
      eventService.getAll.mockRejectedValue(new Error('Network error'))
      const store = useEventStore()
      await store.fetchEvents(1)
      expect(store.error).toBe('Network error')
    })

    it('clears error before each fetch', async () => {
      eventService.getAll.mockRejectedValueOnce(new Error('fail')).mockResolvedValueOnce(partialPage)
      const store = useEventStore()
      await store.fetchEvents(1)
      await store.fetchEvents(1)
      expect(store.error).toBeNull()
    })
  })

  // ── createEvent ───────────────────────────────────────────────────────────

  describe('createEvent', () => {
    it('prepends new event to the list', async () => {
      const created = { id: 'new', title: 'New Event' }
      eventService.getAll.mockResolvedValue(partialPage)
      eventService.create.mockResolvedValue(created)
      const store = useEventStore()
      await store.fetchEvents(1)
      await store.createEvent({ title: 'New Event' })
      expect(store.events[0]).toEqual(created)
      expect(store.events[1]).toEqual(partialPage[0])
    })

    it('returns the created event', async () => {
      const created = { id: 'new', title: 'New Event' }
      eventService.create.mockResolvedValue(created)
      const store = useEventStore()
      const result = await store.createEvent({ title: 'New Event' })
      expect(result).toEqual(created)
    })
  })

  // ── updateEvent ───────────────────────────────────────────────────────────

  describe('updateEvent', () => {
    it('replaces the updated event in the list', async () => {
      const original = { id: '1', title: 'Original' }
      const updated  = { id: '1', title: 'Updated' }
      eventService.getAll.mockResolvedValue([original])
      eventService.update.mockResolvedValue(updated)
      const store = useEventStore()
      await store.fetchEvents(1)
      await store.updateEvent('1', { title: 'Updated' })
      expect(store.events[0].title).toBe('Updated')
    })

    it('returns the updated event', async () => {
      const updated = { id: '1', title: 'Updated' }
      eventService.update.mockResolvedValue(updated)
      const store = useEventStore()
      const result = await store.updateEvent('1', { title: 'Updated' })
      expect(result).toEqual(updated)
    })
  })

  // ── deleteEvent ───────────────────────────────────────────────────────────

  describe('deleteEvent', () => {
    it('removes the event from the list', async () => {
      eventService.getAll.mockResolvedValue([{ id: '1', title: 'To Delete' }])
      eventService.delete.mockResolvedValue(null)
      const store = useEventStore()
      await store.fetchEvents(1)
      await store.deleteEvent('1')
      expect(store.events).toHaveLength(0)
    })
  })

  // ── loadMore ──────────────────────────────────────────────────────────────

  describe('loadMore', () => {
    it('fetches next page when hasMore is true', async () => {
      eventService.getAll.mockResolvedValue(fullPage)
      const store = useEventStore()
      await store.fetchEvents(1)
      store.loadMore()
      expect(eventService.getAll).toHaveBeenCalledWith(2, 20)
    })

    it('does not fetch when hasMore is false', async () => {
      eventService.getAll.mockResolvedValue(partialPage)
      const store = useEventStore()
      await store.fetchEvents(1)
      store.loadMore()
      expect(eventService.getAll).toHaveBeenCalledTimes(1)
    })
  })
})
