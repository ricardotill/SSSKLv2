function getCookie(name: string) {
  if (typeof document === 'undefined') return null
  const match = document.cookie.split('; ').find(c => c.startsWith(name + '='))
  if (!match) return null
  const parts = match.split('=')
  return parts.length > 1 ? decodeURIComponent(parts.slice(1).join('=')) : null
}

export default defineNuxtPlugin(() => {
  const runtimeConfig = useRuntimeConfig()
  const apiBase = runtimeConfig.public.apiBase || ''

  async function fetchWithAuth(path: string, options: RequestInit = {}) {
    const method = (options.method || 'GET').toUpperCase()
    const headers = { ...(options.headers || {}) } as Record<string, string>

    // For non-GET requests include a CSRF header using double-submit cookie pattern
    if (method !== 'GET') {
      const token = getCookie('XSRF-TOKEN') || getCookie('X-CSRF-TOKEN') || null
      if (token) headers['X-XSRF-TOKEN'] = token
    }

    return $fetch(`${apiBase}${path}`, {
      ...options,
      headers,
      credentials: 'include',
    })
  }

  return {
    provide: {
      api: {
        fetchWithAuth,
      },
    },
  }
})
