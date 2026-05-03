import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import EventCard from '@/components/EventCard.vue'

const routerLinkStub = { template: '<a :href="to"><slot /></a>', props: ['to'] }

const baseEvent = {
  id: 'abc-123',
  title: 'Concert Jazz',
  category: 'Concert',
  artistName: 'John Doe Quartet',
  date: '2026-06-15T20:00:00Z',
  location: 'Paris',
  price: 25,
  capacity: 200
}

const mountCard = (overrides = {}) => mount(EventCard, {
  props: { event: { ...baseEvent, ...overrides } },
  global: { stubs: { RouterLink: routerLinkStub } }
})

describe('EventCard', () => {
  it('renders event title', () => {
    expect(mountCard().text()).toContain('Concert Jazz')
  })

  it('renders event category', () => {
    expect(mountCard().text()).toContain('Concert')
  })

  it('renders event location', () => {
    expect(mountCard().text()).toContain('Paris')
  })

  it('renders capacity', () => {
    expect(mountCard().text()).toContain('200 places')
  })

  it('renders artist name when present', () => {
    expect(mountCard().text()).toContain('John Doe Quartet')
  })

  it('does not render artist name when null', () => {
    expect(mountCard({ artistName: null }).text()).not.toContain('John Doe Quartet')
  })

  it('renders Gratuit for free events', () => {
    expect(mountCard({ price: 0 }).text()).toContain('Gratuit')
  })

  it('renders price with euro sign for paid events', () => {
    expect(mountCard({ price: 30 }).text()).toContain('30 €')
  })

  it('links to event detail page', () => {
    expect(mountCard().find('a').attributes('href')).toBe('/events/abc-123')
  })
})
