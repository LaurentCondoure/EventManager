<template>
  <RouterLink :to="`/events/${event.id}`" class="card">
    <div class="card-category">{{ event.category }}</div>
    <h3 class="card-title">{{ event.title }}</h3>
    <p v-if="event.artistName" class="card-artist">{{ event.artistName }}</p>
    <div class="card-meta">
      <span>{{ formatDate(event.date) }}</span>
      <span>{{ event.location }}</span>
    </div>
    <div class="card-footer">
      <span class="card-price">{{ formatPrice(event.price) }}</span>
      <span class="card-capacity">{{ event.capacity }} places</span>
    </div>
  </RouterLink>
</template>

<script setup>
import { RouterLink } from 'vue-router'
import { useFormatters } from '@/composables/useFormatters'

defineProps({
  event: { type: Object, required: true }
})

const { formatDate, formatPrice } = useFormatters()
</script>
