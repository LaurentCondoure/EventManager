<template>
  <div>
    <h1>Événements à venir</h1>

    <div v-if="store.loading && store.events.length === 0" class="loading">Chargement...</div>
    <div v-else-if="store.error" class="error">{{ store.error }}</div>
    <div v-else-if="store.events.length === 0" class="empty">Aucun événement disponible.</div>

    <div v-else class="events-grid">
      <EventCard v-for="event in store.events" :key="event.id" :event="event" />
    </div>

    <div v-if="store.hasMore" class="load-more">
      <button @click="store.loadMore" :disabled="store.loading" class="btn">
        {{ store.loading ? 'Chargement...' : 'Charger plus' }}
      </button>
    </div>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useEventStore } from '@/stores/eventStore'
import EventCard from '@/components/EventCard.vue'

const store = useEventStore()
onMounted(() => store.fetchEvents())
</script>
