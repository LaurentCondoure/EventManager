import { describe, it, expect, vi, beforeEach } from 'vitest'
import { nextTick } from 'vue'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import HomeView from '@/views/HomeView.vue'
import { useEventStore } from '@/stores/eventStore'
import { eventService } from '@/services/apiService'

vi.mock('@/services/apiService', () => ({
  eventService: { getAll: vi.fn() }
}))

const eventCardStub = { template: '<div class="event-card">{{ event.title }}</div>', props: ['event'] }

const mountHome = () => mount(HomeView, {
  global: {
    plugins: [createPinia()],
    stubs: { EventCard: eventCardStub }
  }
})

describe('HomeView', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('shows loading state while fetching', async () => {
    eventService.getAll.mockReturnValue(new Promise(() => {}))
    const wrapper = mountHome()
    await nextTick()
    expect(wrapper.text()).toContain('Chargement')
  })

  it('renders event cards after load', async () => {
    eventService.getAll.mockResolvedValue([
      { id: '1', title: 'Jazz Night', category: 'Concert', date: '2026-06-01', location: 'Paris', price: 20, capacity: 100 }
    ])
    const wrapper = mountHome()
    await flushPromises()
    expect(wrapper.find('.event-card').exists()).toBe(true)
  })

  it('shows empty message when no events', async () => {
    eventService.getAll.mockResolvedValue([])
    const wrapper = mountHome()
    await flushPromises()
    expect(wrapper.text()).toContain('Aucun événement disponible')
  })

  it('shows error message on fetch failure', async () => {
    eventService.getAll.mockRejectedValue(new Error('Network error'))
    const wrapper = mountHome()
    await flushPromises()
    expect(wrapper.text()).toContain('Network error')
  })

  it('shows load more button when hasMore is true', async () => {
    const fullPage = Array.from({ length: 20 }, (_, i) => ({
      id: String(i), title: `Event ${i}`, category: 'Concert',
      date: '2026-06-01', location: 'Paris', price: 0, capacity: 100
    }))
    eventService.getAll.mockResolvedValue(fullPage)
    const wrapper = mountHome()
    await flushPromises()
    expect(wrapper.find('button').exists()).toBe(true)
    expect(wrapper.find('button').text()).toContain('Charger plus')
  })

  it('hides load more button when hasMore is false', async () => {
    eventService.getAll.mockResolvedValue([
      { id: '1', title: 'Solo Event', category: 'Concert', date: '2026-06-01', location: 'Paris', price: 0, capacity: 50 }
    ])
    const wrapper = mountHome()
    await flushPromises()
    expect(wrapper.find('button').exists()).toBe(false)
  })
})
