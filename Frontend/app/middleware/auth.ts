import { navigateTo } from '#app'

export default defineNuxtRouteMiddleware((to) => {
  // allow public pages
  const publicPaths = ['/', '/login', '/register']
  if (publicPaths.includes(to.path)) return

  const token = import.meta.client ? localStorage.getItem('access_token') : null
  if (!token) {
    return navigateTo('/login')
  }
})
