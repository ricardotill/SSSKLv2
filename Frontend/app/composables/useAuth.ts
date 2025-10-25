import { ref } from 'vue'
import { useApi } from './useApi'
import type { ApplicationUserDetailedDto } from '../../types/ApplicationUserDetailedDto'

type LoginRequest = { userName: string; password: string }
type RegisterRequest = { email: string; userName: string; name: string; surname: string; password: string }

export function useAuth() {
  const user = useState<ApplicationUserDetailedDto | null>('auth:user', () => null)
  const isLoading = ref(false)

  const api = useApi()

  async function fetchWithAuth(input: string, init: RequestInit = {}) {
    // Delegate to the centralized API helper which adds CSRF header and credentials
    return api.fetchWithAuth(input, init)
  }

  async function login(payload: LoginRequest) {
    isLoading.value = true
    try {
      // Ask backend to set httpOnly cookies. backend must support ?useCookies=true flag.
      const res: any = await fetchWithAuth(`/api/v1/identity/login?useCookies=true`, {
        method: 'POST',
        body: JSON.stringify(payload),
        headers: { 'Content-Type': 'application/json' }
      })

      // Backend should set httpOnly cookies (refresh cookie, maybe access cookie). Hydrate user.
      await refreshUser()
      return res
    } finally {
      isLoading.value = false
    }
  }

  async function register(payload: RegisterRequest) {
    isLoading.value = true
    try {
      const res = await fetchWithAuth(`/api/v1/identity/register`, {
        method: 'POST',
        body: JSON.stringify(payload),
        headers: { 'Content-Type': 'application/json' }
      })
      return res
    } finally {
      isLoading.value = false
    }
  }

  async function refreshToken() {
    // Cookie-based refresh: backend reads refresh cookie, so no body required.
    try {
      const res: any = await fetchWithAuth(`/api/v1/identity/refresh?useCookies=true`, { method: 'POST' })
      // Backend may update cookies; refresh user state
      await refreshUser()
      return res
    } catch (error_) {
      console.error('refreshToken failed', error_)
      // failed refresh — clear client state
      logout()
      return null
    }
  }

  async function refreshUser() {
    try {
      // Prefer a user-facing endpoint that returns current user details (including userName and saldo)
      const info = await fetchWithAuth(`/api/v1/ApplicationUser/me`, { method: 'GET' })
      user.value = info
      return info
    } catch (error_) {
      console.warn('refreshUser: obscured user fetch failed, falling back to identity manage', error_)
      // try identity manage/info as fallback
      try {
        return await refreshToken();
      } catch (error__) {
        console.error('refreshUser: identity manage fetch also failed', error__)
      }
    }
  }

  async function logout() {
    // Ask server to clear cookies if endpoints exist. Ignore errors.
    try {
      // preferred API logout if available
      await fetchWithAuth(`/api/v1/identity/logout?useCookies=true`, { method: 'POST' })
    } catch (error_) {
      console.warn('server logout failed', error_)
    }

    user.value = null
  }

  // on client init, try to hydrate user via cookies
  if (typeof globalThis !== 'undefined' && !!globalThis.window) {
    // call refreshUser to let cookies authenticate the session
    refreshUser().catch((err) => {
      // log but don't surface
      console.debug('initial refreshUser failed', err)
    })
  }

  return {
    user,
    isLoading,
    login,
    register,
    logout,
    refreshToken: refreshToken,
    fetchWithAuth,
    refreshUser
  }
}
