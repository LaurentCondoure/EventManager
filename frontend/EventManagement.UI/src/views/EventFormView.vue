<template>
  <div class="form-page">
    <RouterLink :to="isEditMode ? `/events/${route.params.id}` : '/'" class="back">← Retour</RouterLink>
    <h1>{{ isEditMode ? 'Modifier l\'événement' : 'Créer un événement' }}</h1>

    <div v-if="loadError" class="error">{{ loadError }}</div>
    <div v-if="loading" class="loading">Chargement...</div>

    <form v-else @submit.prevent="submit" class="event-form">
      <div v-if="error" class="error">{{ error }}</div>

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
        {{ submitting
          ? (isEditMode ? 'Modification...' : 'Création...')
          : (isEditMode ? 'Enregistrer les modifications' : 'Créer l\'événement') }}
      </button>
    </form>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter, RouterLink } from 'vue-router'
import { useEventStore } from '@/stores/eventStore'
import { eventService } from '@/services/apiService'

const route  = useRoute()
const router = useRouter()
const store  = useEventStore()

const isEditMode = computed(() => !!route.params.id)

const categories = ref([])
const loading    = ref(false)
const loadError  = ref(null)
const error      = ref(null)
const submitting = ref(false)

const form = ref({
  title: '', description: '', date: '', location: '',
  category: '', artistName: '', capacity: 100, price: 0
})

onMounted(async () => {
  loading.value = true
  try {
    categories.value = await eventService.getCategories()

    if (isEditMode.value) {
      const event = await eventService.getById(route.params.id)
      form.value = {
        title:       event.title,
        description: event.description,
        date:        new Date(event.date).toISOString().slice(0, 16),
        location:    event.location,
        category:    event.category,
        artistName:  event.artistName ?? '',
        capacity:    event.capacity,
        price:       event.price
      }
    }
  } catch (e) {
    loadError.value = isEditMode.value
      ? 'Impossible de charger l\'événement.'
      : 'Impossible de charger les catégories. Veuillez recharger la page.'
  } finally {
    loading.value = false
  }
})

async function submit() {
  submitting.value = true
  error.value      = null
  try {
    const payload = {
      ...form.value,
      date:       new Date(form.value.date).toISOString(),
      artistName: form.value.artistName || null
    }

    if (isEditMode.value) {
      await store.updateEvent(route.params.id, payload)
      router.push(`/events/${route.params.id}`)
    } else {
      const created = await store.createEvent(payload)
      router.push(`/events/${created.id}`)
    }
  } catch (e) {
    error.value = e.message
  } finally {
    submitting.value = false
  }
}
</script>
