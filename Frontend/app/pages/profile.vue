<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'

definePageMeta({ layout: 'dashboard' })

const items: NavigationMenuItem[][] = [
  [
    {
      label: 'Profiel',
      icon: 'i-lucide-user',
      active: true
    },
    {
      label: 'E-mailadres',
      icon: 'i-lucide-users'
    },
    {
      label: 'Wachtwoord',
      icon: 'i-lucide-key-round'
    },
    {
      label: 'Twee-factor authenticatie',
      icon: 'i-lucide-rectangle-ellipsis'
    },
    {
      label: 'Persoonlijke data',
      icon: 'i-lucide-id-card'
    }
  ],
  []
]
</script>

<template>
    <UContainer class="">
        <UDashboardToolbar>
            <UNavigationMenu :items="items" highlight class="flex-1" />
        </UDashboardToolbar>
    </UContainer>
</template>
