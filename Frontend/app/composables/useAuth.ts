import { ref } from 'vue'
import { useApi } from './useApi'
import type { ApplicationUserDetailedDto } from '../../types/ApplicationUserDetailedDto'

type LoginRequest = { userName: string, password: string }
type RegisterRequest = { email: string, userName: string, name: string, surname: string, password: string }
type ResendConfirmationEmailRequest = { email: string }
type ForgotPasswordRequest = { email: string }
type ResetPasswordRequest = { email: string, resetCode: string, newPassword: string }
type TwoFactorRequest = { enable?: boolean | null, twoFactorCode?: string | null, resetSharedKey?: boolean, resetRecoveryCodes?: boolean, forgetMachine?: boolean }
type TwoFactorResponse = { sharedKey: string, recoveryCodesLeft: number, recoveryCodes?: string[] | null, isTwoFactorEnabled: boolean, isMachineRemembered: boolean }
type InfoRequest = { newEmail?: string | null, newPassword?: string | null, oldPassword?: string | null }
type InfoResponse = { email: string, isEmailConfirmed: boolean }
type ApplicationUserPersonalUpdateDto = {
  phoneNumber?: string | null
  name?: string | null
  surname?: string | null
}

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
      const res = await fetchWithAuth(`/api/v1/identity/login?useCookies=true`, {
        method: 'POST',
        body: JSON.stringify(payload),
        headers: { 'Content-Type': 'application/json' },
      })

      // Backend should set httpOnly cookies (refresh cookie, maybe access cookie). Hydrate user.
      await refreshUser()
      return res
    }
    finally {
      isLoading.value = false
    }
  }

  async function register(payload: RegisterRequest) {
    isLoading.value = true
    try {
      const res = await fetchWithAuth(`/api/v1/identity/register`, {
        method: 'POST',
        body: JSON.stringify(payload),
        headers: { 'Content-Type': 'application/json' },
      })
      return res
    }
    finally {
      isLoading.value = false
    }
  }

  async function refreshToken() {
    // Cookie-based refresh: backend reads refresh cookie, so no body required.
    try {
      const res = await fetchWithAuth(`/api/v1/identity/refresh?useCookies=true`, { method: 'POST' })
      // Backend may update cookies; refresh user state
      await refreshUser()
      return res
    }
    catch (error_) {
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
    }
    catch (error_) {
      console.warn('refreshUser: obscured user fetch failed, falling back to identity manage', error_)
      // try identity manage/info as fallback
      try {
        return await refreshToken()
      }
      catch (error__) {
        console.error('refreshUser: identity manage fetch also failed', error__)
      }
    }
  }

  // Identity helpers from OpenAPI
  async function confirmEmail(userId: string, code: string, changedEmail?: string) {
    const params = new URLSearchParams({ userId, code })
    if (changedEmail) params.append('changedEmail', changedEmail)
    return fetchWithAuth(`/api/v1/identity/confirmEmail?${params.toString()}`, { method: 'GET' })
  }

  async function resendConfirmationEmail(payload: ResendConfirmationEmailRequest) {
    isLoading.value = true
    try {
      return await fetchWithAuth(`/api/v1/identity/resendConfirmationEmail`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
    }
    finally {
      isLoading.value = false
    }
  }

  async function forgotPassword(payload: ForgotPasswordRequest) {
    isLoading.value = true
    try {
      return await fetchWithAuth(`/api/v1/identity/forgotPassword`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
    }
    finally {
      isLoading.value = false
    }
  }

  async function resetPassword(payload: ResetPasswordRequest) {
    isLoading.value = true
    try {
      return await fetchWithAuth(`/api/v1/identity/resetPassword`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
    }
    finally {
      isLoading.value = false
    }
  }

  async function manage2fa(payload: TwoFactorRequest): Promise<TwoFactorResponse> {
    isLoading.value = true
    try {
      const res = await fetchWithAuth(`/api/v1/identity/manage/2fa`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
      // return typed response
      return res as TwoFactorResponse
    }
    finally {
      isLoading.value = false
    }
  }

  async function getManageInfo(): Promise<InfoResponse> {
    const res = await fetchWithAuth(`/api/v1/identity/manage/info`, { method: 'GET' })
    return res as InfoResponse
  }

  async function postManageInfo(payload: InfoRequest): Promise<InfoResponse> {
    isLoading.value = true
    try {
      const res = await fetchWithAuth(`/api/v1/identity/manage/info`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
      // refresh user state since info may have changed
      await refreshUser()
      return res as InfoResponse
    }
    finally {
      isLoading.value = false
    }
  }

  // Update current user's profile (PUT /v1/ApplicationUser/me)
  async function updateMe(payload: ApplicationUserPersonalUpdateDto) {
    isLoading.value = true
    try {
      try {
        const res = await fetchWithAuth(`/v1/ApplicationUser/me`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        })
        // refresh local user data after successful update
        await refreshUser()
        return res
      }
      catch (err: unknown) {
        // $fetch throws an error that may contain `data` with validation details
        // Normalize HttpValidationProblemDetails -> attach `validationErrors` to the thrown error
        const maybeErr = err as { data?: unknown, response?: unknown, status?: number }
        const data = maybeErr.data || maybeErr.response || null
        if (data && typeof data === 'object' && 'errors' in (data as Record<string, unknown>)) {
          const d = data as Record<string, unknown>
          const validationErrors = (d['errors'] as Record<string, string[]>) || {}
          interface ValidationError extends Error { validationErrors?: Record<string, string[]>, status?: number }
          const ex = new Error((d['title'] as string) || 'Valideringsfout') as ValidationError
          ex.validationErrors = validationErrors
          ex.status = typeof maybeErr.status === 'number' ? maybeErr.status : undefined
          throw ex
        }
        // otherwise rethrow original
        throw err
      }
    }
    finally {
      isLoading.value = false
    }
  }

  async function logout() {
    // Ask server to clear cookies if endpoints exist. Ignore errors.
    try {
      // preferred API logout if available
      await fetchWithAuth(`/api/v1/identity/logout?useCookies=true`, { method: 'POST' })
    }
    catch (error_) {
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
    refreshUser,
    // identity helpers
    confirmEmail,
    resendConfirmationEmail,
    forgotPassword,
    resetPassword,
    manage2fa,
    getManageInfo,
    postManageInfo,
    updateMe,
  }
}
