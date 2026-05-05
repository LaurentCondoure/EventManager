import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/views/HomeView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/',             component: HomeView },
    { path: '/events/:id',       component: () => import('@/views/EventDetailView.vue') },
    { path: '/create',           component: () => import('@/views/EventFormView.vue') },
    { path: '/events/:id/edit',  component: () => import('@/views/EventFormView.vue') },
    { path: '/search',       component: () => import('@/views/SearchView.vue') }
  ]
})

export default router
