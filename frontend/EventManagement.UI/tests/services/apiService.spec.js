import { describe, it, expect, vi, beforeEach } from 'vitest'
import { eventService, commentService } from '@/services/apiService'

const mockFetch = (status, body, ok = true) =>
  vi.fn().mockResolvedValue({
    ok,
    status,
    json:       () => Promise.resolve(body),
    statusText: 'Error'
  })

describe('apiService', () => {
  beforeEach(() => vi.unstubAllGlobals())

  describe('request', () => {
    it('returns parsed JSON on success', async () => {
      vi.stubGlobal('fetch', mockFetch(200, { id: '1' }))
      const result = await eventService.getById('1')
      expect(result).toEqual({ id: '1' })
    })

    it('returns null on 204', async () => {
      vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, status: 204 }))
      const result = await eventService.getById('1')
      expect(result).toBeNull()
    })

    it('throws error using detail field when response is not ok', async () => {
      vi.stubGlobal('fetch', mockFetch(400, { detail: 'Validation failed' }, false))
      await expect(eventService.getById('1')).rejects.toThrow('Validation failed')
    })

    it('throws error using message field as fallback', async () => {
      vi.stubGlobal('fetch', mockFetch(500, { message: 'Server error' }, false))
      await expect(eventService.getById('1')).rejects.toThrow('Server error')
    })

    it('throws HTTP status when body has neither detail nor message', async () => {
      vi.stubGlobal('fetch', mockFetch(503, {}, false))
      await expect(eventService.getById('1')).rejects.toThrow('HTTP 503')
    })
  })

  describe('eventService', () => {
    beforeEach(() => {
      vi.stubGlobal('fetch', mockFetch(200, []))
    })

    it('getAll calls correct URL', async () => {
      await eventService.getAll(2, 10)
      expect(fetch).toHaveBeenCalledWith('/api/events?page=2&pageSize=10', expect.any(Object))
    })

    it('getById calls correct URL', async () => {
      await eventService.getById('abc')
      expect(fetch).toHaveBeenCalledWith('/api/events/abc', expect.any(Object))
    })

    it('getFull calls correct URL', async () => {
      await eventService.getFull('abc')
      expect(fetch).toHaveBeenCalledWith('/api/events/abc/full', expect.any(Object))
    })

    it('search encodes query and calls correct URL', async () => {
      await eventService.search('jazz festival', 1)
      expect(fetch).toHaveBeenCalledWith('/api/events/search?q=jazz%20festival&page=1', expect.any(Object))
    })

    it('create sends POST with body', async () => {
      const data = { title: 'Test' }
      await eventService.create(data)
      expect(fetch).toHaveBeenCalledWith('/api/events', expect.objectContaining({
        method: 'POST',
        body: JSON.stringify(data)
      }))
    })

    it('update sends PUT to correct URL with body', async () => {
      const data = { title: 'Updated' }
      await eventService.update('abc', data)
      expect(fetch).toHaveBeenCalledWith('/api/events/abc', expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify(data)
      }))
    })

    it('delete sends DELETE to correct URL', async () => {
      vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, status: 204 }))
      await eventService.delete('abc')
      expect(fetch).toHaveBeenCalledWith('/api/events/abc', expect.objectContaining({
        method: 'DELETE'
      }))
    })
  })

  describe('commentService', () => {
    beforeEach(() => {
      vi.stubGlobal('fetch', mockFetch(200, []))
    })

    it('getByEvent calls correct URL', async () => {
      await commentService.getByEvent('event-id')
      expect(fetch).toHaveBeenCalledWith('/api/events/event-id/comments', expect.any(Object))
    })

    it('create sends POST to correct URL', async () => {
      const data = { userName: 'Alice', rating: 5 }
      await commentService.create('event-id', data)
      expect(fetch).toHaveBeenCalledWith('/api/events/event-id/comments', expect.objectContaining({
        method: 'POST',
        body: JSON.stringify(data)
      }))
    })
  })
})
