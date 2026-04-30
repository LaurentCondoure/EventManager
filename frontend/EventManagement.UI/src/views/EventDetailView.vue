<template>
  <div v-if="loading" class="loading">Chargement...</div>
  <div v-else-if="error" class="error">{{ error }}</div>

  <div v-else-if="data">
    <RouterLink to="/" class="back">← Retour</RouterLink>

    <article class="event-detail">
      <span class="tag">{{ data.event.category }}</span>
      <h1>{{ data.event.title }}</h1>
      <p v-if="data.event.artistName" class="artist">{{ data.event.artistName }}</p>
      <div class="event-meta">
        <span>{{ formatDate(data.event.date) }}</span>
        <span>{{ data.event.location }}</span>
        <span>{{ formatPrice(data.event.price) }}</span>
        <span>{{ data.event.capacity }} places</span>
      </div>
      <p class="description">{{ data.event.description }}</p>
    </article>

    <section class="comments">
      <h2>Commentaires ({{ data.comments.length }})</h2>

      <form @submit.prevent="submitComment" class="comment-form">
        <input v-model="form.userName" placeholder="Votre nom" required />
        <div class="rating">
          <label>Note :</label>
          <select v-model.number="form.rating">
            <option v-for="n in 5" :key="n" :value="n">{{ n }} ★</option>
          </select>
        </div>
        <textarea v-model="form.text" placeholder="Votre commentaire (optionnel)" rows="3" />
        <button type="submit" :disabled="submitting" class="btn">
          {{ submitting ? 'Envoi...' : 'Publier' }}
        </button>
      </form>

      <div v-if="data.comments.length === 0" class="empty">Aucun commentaire pour l'instant.</div>
      <div v-else class="comment-list">
        <div v-for="c in data.comments" :key="c.id" class="comment">
          <div class="comment-header">
            <strong>{{ c.userName }}</strong>
            <span class="stars">{{ '★'.repeat(c.rating) }}{{ '☆'.repeat(5 - c.rating) }}</span>
            <span class="date">{{ formatDate(c.createdAt) }}</span>
          </div>
          <p v-if="c.text">{{ c.text }}</p>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { eventService, commentService } from '@/services/apiService'
import { useFormatters } from '@/composables/useFormatters'

const route     = useRoute()
const data      = ref(null)
const loading   = ref(false)
const error     = ref(null)
const submitting = ref(false)

const form = ref({ userName: '', rating: 5, text: '' })

onMounted(async () => {
  loading.value = true
  try {
    data.value = await eventService.getFull(route.params.id)
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
})

async function submitComment() {
  submitting.value = true
  try {
    await commentService.create(route.params.id, form.value)
    //call 
    data.value = await eventService.getFull(route.params.id)
    form.value = { userName: '', rating: 5, text: '' }
  } finally {
    submitting.value = false
  }
}

const { formatDate, formatPrice } = useFormatters()
</script>
