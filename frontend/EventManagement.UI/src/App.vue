<template>
  <header class="header">
    <nav>
      <RouterLink to="/">Événements</RouterLink>
      <RouterLink to="/create">Créer</RouterLink>
      <RouterLink to="/search">Recherche</RouterLink>
    </nav>
  </header>

  <main class="main">
    <RouterView />
  </main>
</template>

<script setup>
import { RouterLink, RouterView } from 'vue-router'
</script>
