<script setup lang="ts">
import { ref, computed } from 'vue'
import { useUser } from '@/composables/useUser'
import { useRoute } from '#imports'

const { user } = useUser()

const isUserAvailable = computed(() => {
  const u = user?.value ?? null
  return !!(u && (u.userName || u.fullName || u.email))
})

const open = ref(false)
const route = useRoute()

const baseMenu = [
  { label: 'Home', to: '/', icon: 'i-lucide-home' },
  { label: 'Bestellen', to: '/bestellen', icon: 'i-lucide-shopping-cart' },
  { label: 'Geschiedenis', to: '/geschiedenis', icon: 'i-lucide-clock' },
  { label: 'Achievements', to: '/achievements', icon: 'i-lucide-award' },
  { label: 'Gebruikers', to: '/gebruikers', icon: 'i-lucide-users' },
]

const menuItems = computed(() => baseMenu.map(i => ({
  ...i,
  active: route.path === i.to,
})))

const activeTitle = computed(() => {
  const active = menuItems.value.find(i => i.active)
  return active?.label ?? 'Dashboard'
})
</script>

<template>
  <UDashboardGroup>
    <Sidebar
      v-model:open="open"
      :items="menuItems"
    />

    <UDashboardPanel>
      <template #header>
        <UDashboardNavbar :title="activeTitle">
          <template #right>
            <CreditView
              v-if="isUserAvailable"
              :credit="user?.saldo ?? 0"
            />
            <UButton
              v-else
              color="primary"
              variant="solid"
              :to="{ name: 'login' }"
              label="Inloggen"
            />
          </template>
        </UDashboardNavbar>
      </template>

      <template #body>
        <main class="">
          <slot />
        </main>
      </template>
    </UDashboardPanel>
  </UDashboardGroup>
</template>

<style scoped>
/* Rely on Nuxt UI for styling */
</style>
