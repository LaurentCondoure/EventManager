import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia } from 'pinia'
import EventFormView from '@/views/EventFormView.vue'
import { eventService } from '@/services/apiService'

const mockPush = vi.fn()
let mockRouteParams = {}

vi.mock('vue-router', () => ({
  useRoute:   () => ({ params: mockRouteParams }),
  useRouter:  () => ({ push: mockPush }),
  RouterLink: { template: '<a><slot /></a>' }
}))

vi.mock('@/services/apiService', () => ({
  eventService: {
    getCategories: vi.fn(),
    getAll:        vi.fn().mockResolvedValue([]),
    create:        vi.fn(),
    getById:       vi.fn(),
    update:        vi.fn()
  }
}))

const categories = ['Concert', 'Théâtre', 'Exposition']

const mountForm = () => mount(EventFormView, {
  global: { plugins: [createPinia()] }
})

describe('EventFormView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockRouteParams = {}
  })

  // ── Category loading ───────────────────────────────────────────────────────

  describe('category loading', () => {
    it('disables select while categories are loading', () => {
      eventService.getCategories.mockResolvedValue(categories)
      const wrapper = mountForm()
      expect(wrapper.find('select').attributes()).toHaveProperty('disabled')
    })

    it('enables select with options after categories load', async () => {
      eventService.getCategories.mockResolvedValue(categories)
      const wrapper = mountForm()
      await flushPromises()
      const options = wrapper.findAll('option').filter(o => o.element.value !== '')
      expect(options).toHaveLength(categories.length)
    })

    it('shows error when categories fetch fails', async () => {
      eventService.getCategories.mockRejectedValue(new Error('Network error'))
      const wrapper = mountForm()
      await flushPromises()
      expect(wrapper.text()).toContain('Impossible de charger les catégories')
    })

    it('keeps select disabled when categories fetch fails', async () => {
      eventService.getCategories.mockRejectedValue(new Error('Network error'))
      const wrapper = mountForm()
      await flushPromises()
      expect(wrapper.find('select').attributes()).toHaveProperty('disabled')
    })
  })

  // ── Create mode ────────────────────────────────────────────────────────────

  describe('create mode', () => {
    beforeEach(() => {
      eventService.getCategories.mockResolvedValue(categories)
    })

    it('shows create title', async () => {
      const wrapper = mountForm()
      await flushPromises()
      expect(wrapper.text()).toContain('Créer un événement')
    })

    it('redirects to new event on success', async () => {
      eventService.create.mockResolvedValue({ id: 'new-id' })
      const wrapper = mountForm()
      await flushPromises()

      await wrapper.find('input[type="datetime-local"]').setValue('2026-12-01T20:00')
      await wrapper.find('select').setValue('Concert')
      await wrapper.find('form').trigger('submit')
      await flushPromises()

      expect(mockPush).toHaveBeenCalledWith('/events/new-id')
    })

    it('shows error message on submission failure', async () => {
      eventService.create.mockRejectedValue(new Error('Erreur serveur'))
      const wrapper = mountForm()
      await flushPromises()

      await wrapper.find('input[type="datetime-local"]').setValue('2026-12-01T20:00')
      await wrapper.find('select').setValue('Concert')
      await wrapper.find('form').trigger('submit')
      await flushPromises()

      expect(wrapper.text()).toContain('Erreur serveur')
    })
  })

  // ── Edit mode ──────────────────────────────────────────────────────────────

  describe('edit mode', () => {
    const existingEvent = {
      id: 'event-123',
      title: 'Jazz Night',
      description: 'A great show',
      date: '2026-06-15T20:00:00Z',
      location: 'Paris',
      category: 'Concert',
      artistName: 'Miles Davis',
      capacity: 200,
      price: 25
    }

    beforeEach(() => {
      mockRouteParams = { id: 'event-123' }
      eventService.getCategories.mockResolvedValue(categories)
      eventService.getById.mockResolvedValue(existingEvent)
    })

    it('shows edit title', async () => {
      const wrapper = mountForm()
      await flushPromises()
      expect(wrapper.text()).toContain("Modifier l'événement")
    })

    it('pre-fills form with existing event data', async () => {
      const wrapper = mountForm()
      await flushPromises()
      const titleInput = wrapper.find('input[maxlength="200"]')
      expect(titleInput.element.value).toBe('Jazz Night')
    })

    it('calls update on submit and redirects to event detail', async () => {
      eventService.update.mockResolvedValue(existingEvent)
      const wrapper = mountForm()
      await flushPromises()

      await wrapper.find('form').trigger('submit')
      await flushPromises()

      expect(eventService.update).toHaveBeenCalledWith(
        'event-123',
        expect.objectContaining({ title: 'Jazz Night' })
      )
      expect(mockPush).toHaveBeenCalledWith('/events/event-123')
    })

    it('does not call create in edit mode', async () => {
      eventService.update.mockResolvedValue(existingEvent)
      const wrapper = mountForm()
      await flushPromises()

      await wrapper.find('form').trigger('submit')
      await flushPromises()

      expect(eventService.create).not.toHaveBeenCalled()
    })

    it('shows error when event fails to load', async () => {
      eventService.getById.mockRejectedValue(new Error('Not found'))
      const wrapper = mountForm()
      await flushPromises()
      expect(wrapper.text()).toContain("Impossible de charger l'événement")
    })
  })
})
