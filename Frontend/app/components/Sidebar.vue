<script setup lang="ts">
import { computed } from 'vue'
import type { PropType } from 'vue'
import type { NavigationMenuItem } from '@nuxt/ui'
import { useUser } from '~/composables/useUser'
import { useAuth } from '~/composables/useAuth'

const props = defineProps({
  // accept full NavigationMenuItem so callers can set `active`, `icon`, `badge`, etc.
  items: { type: Array as PropType<NavigationMenuItem[]>, default: () => [] },
  open: { type: Boolean, default: false },
})

const emit = defineEmits(['close', 'update:open'] as const)

const localOpen = computed({
  get: () => props.open,
  set: (v: boolean) => emit('update:open', v),
})

// pass-through items (UNavigationMenu expects NavigationMenuItem[])
const navItems = computed<NavigationMenuItem[]>(() => props.items as NavigationMenuItem[])

const { user } = useUser()
const { logout } = useAuth()

const isUserAvailable = computed(() => {
  const u = user?.value ?? null
  return !!(u && (u.userName || u.fullName || u.email))
})

const displayName = computed(() => {
  const u = user?.value ?? null
  return (u && (u.userName || u.fullName || u.email)) ? (u.userName ?? u.fullName ?? u.email) : 'Guest'
})

const avatarInitial = computed(() => {
  const name = displayName.value || ''
  return name.length > 0 ? String(name).charAt(0) : '?'
})

const saldoText = computed(() => {
  const u = user?.value ?? null
  return u && (u as any).saldo != null ? `${(u as any).saldo} credits` : ''
})

function onLogout() {
  logout()
}
</script>

<template>
  <UDashboardSidebar
    v-model:open="localOpen"
    :toggle="{ color: 'neutral', variant: 'ghost' }"
    :ui="{ header: 'px-4 py-3', body: 'px-2 py-3', footer: 'px-4 py-3' }"
  >
    <template #header="{ collapsed }">
      <h2
        v-if="!collapsed"
        class="text-lg font-semibold"
      >
        SSSKL
      </h2>
      <UBadge
        label="v2"
        :variant="'subtle'"
        color="primary"
      />
    </template>
    <template #default="{ collapsed }">
      <!-- Pass props.items directly to avoid computed/ref wrapper interfering with UNavigationMenu navigation -->
      <UNavigationMenu
        class="mt-2"
        :items="props.items"
        :collapsed="collapsed"
        orientation="vertical"
      />
    </template>

    <template #footer="{ collapsed }">
      <div class="w-full">
        <template v-if="isUserAvailable">
          <UButton
            :avatar="{ text: avatarInitial }"
            :label="displayName"
            color="neutral"
            variant="ghost"
            class="w-full mb-2"
            :to="{ name: 'profile' }"
            :block="collapsed"
          />

          <UButton
            label="Uitloggen"
            color="error"
            variant="ghost"
            class="w-full"
            @click="onLogout"
          />
        </template>
        <template v-else>
          <UButton
            label="Login"
            color="primary"
            variant="solid"
            class="w-full"
            :block="collapsed"
            :to="{ name: 'login' }"
          />
        </template>
      </div>
    </template>
  </UDashboardSidebar>
</template>

<style scoped>
/* No custom styles — Nuxt UI handles look & feel */
</style>
