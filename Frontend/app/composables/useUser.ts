import { computed, type Ref } from 'vue'
import { useAuth } from './useAuth'
import type { ApplicationUserDetailedDto } from '../../types/ApplicationUserDetailedDto'

// Real user composable that delegates to `useAuth` and exposes a typed user ref.
export function useUser() {
  const { user, refreshUser } = useAuth()

  // Refine the user ref type to the detailed DTO
  const typedUser = user as unknown as Ref<ApplicationUserDetailedDto | null>

  const hasCredit = computed(() => !!typedUser.value?.saldo)

  return { user: typedUser, hasCredit, refreshUser }
}
