import { describe, it, expect, vi, beforeEach } from 'vitest'
import { nextTick } from 'vue'
import { mount, flushPromises } from '@vue/test-utils'
import EventDetailView from '@/views/EventDetailView.vue'
import { eventService, commentService } from '@/services/apiService'

vi.mock('vue-router', () => ({
  useRoute:   () => ({ params: { id: 'event-id' } }),
  RouterLink: { template: '<a><slot /></a>' }
}))

vi.mock('@/services/apiService', () => ({
  eventService:  { getFull: vi.fn() },
  commentService: { create: vi.fn() }
}))

const fullData = {
  event: {
    id: 'event-id', title: 'Jazz Night', category: 'Concert',
    artistName: 'Miles Davis Trio', date: '2026-06-15T20:00:00Z',
    location: 'Paris', price: 25, capacity: 200,
    description: 'An unforgettable jazz evening.'
  },
  comments: [
    { id: 'c1', userName: 'Alice', rating: 5, text: 'Magnifique!', createdAt: '2026-05-01T10:00:00Z' }
  ]
}

const mountView = () => mount(EventDetailView)

describe('EventDetailView', () => {
  beforeEach(() => vi.clearAllMocks())

  it('shows loading state on mount', async () => {
    eventService.getFull.mockReturnValue(new Promise(() => {}))
    const wrapper = mountView()
    await nextTick()
    expect(wrapper.text()).toContain('Chargement')
  })

  it('renders event details after load', async () => {
    eventService.getFull.mockResolvedValue(fullData)
    const wrapper = mountView()
    await flushPromises()
    expect(wrapper.text()).toContain('Jazz Night')
    expect(wrapper.text()).toContain('Miles Davis Trio')
    expect(wrapper.text()).toContain('An unforgettable jazz evening.')
  })

  it('renders comments after load', async () => {
    eventService.getFull.mockResolvedValue(fullData)
    const wrapper = mountView()
    await flushPromises()
    expect(wrapper.text()).toContain('Alice')
    expect(wrapper.text()).toContain('Magnifique!')
  })

  it('shows empty message when no comments', async () => {
    eventService.getFull.mockResolvedValue({ ...fullData, comments: [] })
    const wrapper = mountView()
    await flushPromises()
    expect(wrapper.text()).toContain('Aucun commentaire')
  })

  it('shows error message on load failure', async () => {
    eventService.getFull.mockRejectedValue(new Error('Event not found'))
    const wrapper = mountView()
    await flushPromises()
    expect(wrapper.text()).toContain('Event not found')
  })

  it('submits comment and refreshes data', async () => {
    eventService.getFull.mockResolvedValue(fullData)
    commentService.create.mockResolvedValue({})
    const wrapper = mountView()
    await flushPromises()

    await wrapper.find('input[placeholder="Votre nom"]').setValue('Bob')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(commentService.create).toHaveBeenCalledWith('event-id', expect.objectContaining({ userName: 'Bob' }))
    expect(eventService.getFull).toHaveBeenCalledTimes(2)
  })

  it('resets form after comment submission', async () => {
    eventService.getFull.mockResolvedValue(fullData)
    commentService.create.mockResolvedValue({})
    const wrapper = mountView()
    await flushPromises()

    await wrapper.find('input[placeholder="Votre nom"]').setValue('Bob')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('input[placeholder="Votre nom"]').element.value).toBe('')
  })
})
