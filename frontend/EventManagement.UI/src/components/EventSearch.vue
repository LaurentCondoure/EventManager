<template>
  <div class="search">
    <div class="search-bar">
      <input
        v-model="query"
        @keyup.enter="search"
        placeholder="Rechercher un événement..."
        class="search-input"
      />
      <button @click="search" :disabled="loading" class="btn">Rechercher</button>
    </div>

    <div v-if="loading" class="loading">Recherche en cours...</div>

    <div v-else-if="results.length" class="search-results">
      <p class="results-count">{{ results.length }} résultat(s)</p>
      <div class="events-grid">
        <EventCard v-for="event in results" :key="event.id" :event="event" />
      </div>
    </div>

    <div v-else-if="searched" class="empty">Aucun résultat pour "{{ query }}"</div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { eventService } from '@/services/apiService'
import EventCard from './EventCard.vue'

const query   = ref('')
const results = ref([])
const loading = ref(false)
const searched = ref(false)

async function search() {
  if (!query.value.trim()) return
  loading.value  = true
  searched.value = false
  try {
    results.value  = await eventService.search(query.value)
    searched.value = true
  } finally {
    loading.value = false
  }
}
</script>
