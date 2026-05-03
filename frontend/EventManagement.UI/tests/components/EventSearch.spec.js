import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import EventSearch from '@/components/EventSearch.vue'
import { eventService } from '@/services/apiService'

vi.mock('@/services/apiService', () => ({
  eventService: { search: vi.fn() }
}))

const eventCardStub = { template: '<div class="event-card">{{ event.title }}</div>', props: ['event'] }

const mountSearch = () => mount(EventSearch, {
  global: { stubs: { EventCard: eventCardStub } }
})

describe('EventSearch', () => {
  beforeEach(() => vi.clearAllMocks())

  it('does not call search when query is empty', async () => {
    const wrapper = mountSearch()
    await wrapper.find('button').trigger('click')
    expect(eventService.search).not.toHaveBeenCalled()
  })

  it('calls search service on button click', async () => {
    eventService.search.mockResolvedValue([])
    const wrapper = mountSearch()
    await wrapper.find('input').setValue('jazz')
    await wrapper.find('button').trigger('click')
    await flushPromises()
    expect(eventService.search).toHaveBeenCalledWith('jazz')
  })

  it('calls search service on Enter key', async () => {
    eventService.search.mockResolvedValue([])
    const wrapper = mountSearch()
    await wrapper.find('input').setValue('jazz')
    await wrapper.find('input').trigger('keyup.enter')
    await flushPromises()
    expect(eventService.search).toHaveBeenCalledWith('jazz')
  })

  it('displays result count after search', async () => {
    eventService.search.mockResolvedValue([
      { id: '1', title: 'Jazz Festival', category: 'Concert', date: '2026-06-01', location: 'Paris', price: 0, capacity: 100 }
    ])
    const wrapper = mountSearch()
    await wrapper.find('input').setValue('jazz')
    await wrapper.find('button').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('1 résultat')
  })

  it('shows empty message when no results', async () => {
    eventService.search.mockResolvedValue([])
    const wrapper = mountSearch()
    await wrapper.find('input').setValue('jazz')
    await wrapper.find('button').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Aucun résultat')
  })

  it('does not show empty message before first search', () => {
    expect(mountSearch().text()).not.toContain('Aucun résultat')
  })
})
