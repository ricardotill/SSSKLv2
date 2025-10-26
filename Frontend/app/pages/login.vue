<script setup lang="ts">
import { ref } from 'vue'
import { useAuth } from '~/composables/useAuth'
import { navigateTo } from '#app'

definePageMeta({ layout: 'default' })

const userName = ref('')
const password = ref('')
const error = ref<string | null>(null)
const loading = ref(false)

const { login } = useAuth()

async function onSubmit() {
  error.value = null
  loading.value = true
  try {
    await login({ userName: userName.value, password: password.value })
    navigateTo('/')
  }
  catch (err) {
    let msg = 'Inloggen mislukt'
    // attempt to read common error shapes safely
    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    // @ts-ignore - `err` may be unknown shape from $fetch
    if (err?.data?.message) msg = err.data.message
    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    // @ts-ignore
    else if (err?.data?.title) msg = err.data.title
    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    // @ts-ignore
    else if (err?.message) msg = err.message
    error.value = msg
  }
  finally {
    loading.value = false
  }
}
</script>

<template>
  <UForm @submit.prevent="onSubmit">
    <UCard
      class="max-w-md mx-auto mt-12 p-4"
      variant="subtle"
    >
      <template #header>
        <div class="flex items-center gap-3">
          <UButton
            icon="i-lucide-arrow-left"
            size="xl"
            color="neutral"
            variant="outline"
            type="button"
            aria-label="Terug"
            :to="{ name: 'index' }"
          />
          <h1 class="text-xl font-semibold">
            Inloggen
          </h1>
        </div>
      </template>

      <div class="mb-4">
        <UFormField
          name="w-auto"
          label="Gebruikersnaam"
          size="xl"
        >
          <UInput
            id="userName"
            v-model="userName"
            class="w-full"
            type="text"
            placeholder="Gebruikersnaam"
          />
        </UFormField>
      </div>

      <div class="mb-4">
        <UFormField
          name="password"
          label="Wachtwoord"
          size="xl"
        >
          <UInput
            id="password"
            v-model="password"
            class="w-full"
            type="password"
            placeholder="Wachtwoord"
          />
        </UFormField>
      </div>

      <div
        v-if="error"
        class="text-red-600"
      >
        {{ error }}
      </div>

      <template #footer>
        <div class="flex justify-end">
          <UButton
            class="mr-auto"
            label="Nog geen account?"
            color="primary"
            variant="ghost"
            type="button"
            :to="{ name: 'register' }"
          />
          <UButton
            class="justify-end"
            type="submit"
            :loading="loading"
            loading-icon="i-lucide-loader"
            label="Inloggen"
            color="primary"
          />
        </div>
      </template>
    </UCard>
  </UForm>
</template>
