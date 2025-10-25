export function useApi() {
  const nuxt = useNuxtApp()
  return nuxt.$api as { fetchWithAuth: (path: string, options?: RequestInit) => Promise<any> }
}
