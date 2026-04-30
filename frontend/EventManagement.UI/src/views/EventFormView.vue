<template>
  <div class="form-page">
    <RouterLink to="/" class="back">← Retour</RouterLink>
    <h1>Créer un événement</h1>

    <div v-if="error" class="error">{{ error }}</div>

    <form @submit.prevent="submit" class="event-form">
      <div class="field">
        <label>Titre *</label>
        <input v-model="form.title" required maxlength="200" />
      </div>

      <div class="field">
        <label>Description *</label>
        <textarea v-model="form.description" required maxlength="2000" rows="4" />
      </div>

      <div class="field">
        <label>Date *</label>
        <input v-model="form.date" type="datetime-local" required />
      </div>

      <div class="field">
        <label>Lieu *</label>
        <input v-model="form.location" required maxlength="200" />
      </div>

      <div class="field">
        <label>Catégorie *</label>
        <select v-model="form.category" required :disabled="categories.length === 0">
          <option value="" disabled>{{ categories.length === 0 ? 'Chargement...' : 'Sélectionner' }}</option>
          <option v-for="cat in categories" :key="cat" :value="cat">{{ cat }}</option>
        </select>
      </div>

      <div class="field">
        <label>Artiste / Troupe</label>
        <input v-model="form.artistName" maxlength="200" />
      </div>

      <div class="field-row">
        <div class="field">
          <label>Capacité *</label>
          <input v-model.number="form.capacity" type="number" min="1" required />
        </div>
        <div class="field">
          <label>Prix (€) *</label>
          <input v-model.number="form.price" type="number" min="0" step="0.01" required />
        </div>
      </div>

      <button type="submit" :disabled="submitting" class="btn">
        {{ submitting ? 'Création...' : 'Créer l\'événement' }}
      </button>
    </form>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { useEventStore } from '@/stores/eventStore'
import { eventService } from '@/services/apiService'

const router     = useRouter()
const store      = useEventStore()
const error      = ref(null)
const submitting = ref(false)

const categories = ref([])
onMounted(async () => {
  try {
    categories.value = await eventService.getCategories()
  } catch {
    error.value = 'Impossible de charger les catégories. Veuillez recharger la page.'
  }
})

const form = ref({
  title: '', description: '', date: '', location: '',
  category: '', artistName: '', capacity: 100, price: 0
})

async function submit() {
  submitting.value = true
  error.value      = null
  try {
    const payload = {
      ...form.value,
      date: new Date(form.value.date).toISOString(),
      artistName: form.value.artistName || null
    }
    const created = await store.createEvent(payload)
    router.push(`/events/${created.id}`)
  } catch (e) {
    error.value = e.message
  } finally {
    submitting.value = false
  }
}
</script>
