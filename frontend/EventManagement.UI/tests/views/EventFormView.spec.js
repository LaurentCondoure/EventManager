import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia } from 'pinia'
import EventFormView from '@/views/EventFormView.vue'
import { eventService } from '@/services/apiService'

const mockPush = vi.fn()

vi.mock('vue-router', () => ({
  useRouter:  () => ({ push: mockPush }),
  RouterLink: { template: '<a><slot /></a>' }
}))

vi.mock('@/services/apiService', () => ({
  eventService: {
    getCategories: vi.fn(),
    getAll:        vi.fn().mockResolvedValue([]),
    create:        vi.fn()
  }
}))

const categories = ['Concert', 'Théâtre', 'Exposition']

const mountForm = () => mount(EventFormView, {
  global: { plugins: [createPinia()] }
})

describe('EventFormView', () => {
  beforeEach(() => vi.clearAllMocks())

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

  // ── Form submission ────────────────────────────────────────────────────────

  describe('form submission', () => {
    beforeEach(() => {
      eventService.getCategories.mockResolvedValue(categories)
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
})
