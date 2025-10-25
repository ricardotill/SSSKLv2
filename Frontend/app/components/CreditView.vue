<script setup lang="ts">
import { computed } from 'vue'
const props = defineProps<{ credit: number }>()

const formatted = computed(() => {
  const c = props.credit ?? 0
  return "€ " + c.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
})

const isNegative = computed(() => (props.credit ?? 0) < 0)
</script>

<template>
  <div class="flex items-center gap-2">
    <UIcon name="i-lucide-credit-card" class="size-4 text-muted" />
    <UBadge :label="formatted" :variant="'subtle'" :color="isNegative ? 'error' : 'neutral'" />
  </div>
</template>

<style scoped>
/* small adjustments if needed */
</style>
