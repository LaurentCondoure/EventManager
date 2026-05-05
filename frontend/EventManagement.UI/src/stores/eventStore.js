import { defineStore } from 'pinia'
import { ref } from 'vue'
import { eventService } from '@/services/apiService'

export const useEventStore = defineStore('event', () => {
  const events      = ref([])
  const loading     = ref(false)
  const error       = ref(null)
  const currentPage = ref(1)
  const hasMore     = ref(true)

  async function fetchEvents(page = 1, pageSize = 20) {
    loading.value = true
    error.value   = null
    try {
      const data = await eventService.getAll(page, pageSize)
      events.value  = page === 1 ? data : [...events.value, ...data]
      currentPage.value = page
      hasMore.value = data.length === pageSize
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function createEvent(data) {
    const created = await eventService.create(data)
    events.value = [created, ...events.value]
    return created
  }

  function loadMore() {
    if (!loading.value && hasMore.value)
      fetchEvents(currentPage.value + 1)
  }

  return { events, loading, error, hasMore, fetchEvents, createEvent, loadMore }
})
