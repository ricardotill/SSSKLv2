<script setup lang="ts">
import { ref } from 'vue'
import { useAuth } from '~/composables/useAuth'
import { navigateTo } from '#app'

definePageMeta({ layout: 'default' })

const userName = ref('')
const name = ref('')
const surname = ref('')
const email = ref('')
const password = ref('')
const error = ref<string | null>(null)
const loading = ref(false)

const { register } = useAuth()

async function onSubmit() {
  error.value = null
  loading.value = true
  try {
    await register({ email: email.value, userName: userName.value, name: name.value, surname: surname.value, password: password.value })
    navigateTo('/login')
  } catch (err) {
    let msg = 'Registration failed'
    try {
      // @ts-ignore
      if (err && err.data && err.data.message) msg = err.data.message
      // @ts-ignore
      else if (err && err.message) msg = err.message
    } catch (error_) {
      /* ignore */
    }
    error.value = msg
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <UForm @submit.prevent="onSubmit">
  <UCard class="max-w-md mx-auto mt-12 p-4"  variant="subtle">
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
          <h1 class="text-xl font-semibold">Registreren</h1>
        </div>
      </template>
      <div>
        <div class="mb-4">
          <UFormField name="userName" label="Gebruikersnaam" size="xl">
            <UInput id="userName" v-model="userName" class="w-full" type="text" placeholder="Gebruikersnaam" />
          </UFormField>
        </div>

        <div class="mb-4">
          <UFormField name="name" label="Voornaam" size="xl">
            <UInput id="name" v-model="name" class="w-full" type="text" placeholder="Voornaam" />
          </UFormField>
        </div>

        <div class="mb-4">
          <UFormField name="surname" label="Achternaam" size="xl">
            <UInput id="surname" v-model="surname" class="w-full" type="text" placeholder="Achternaam" />
          </UFormField>
        </div>

        <div class="mb-4">
          <UFormField name="email" label="E-mailadres" size="xl">
            <UInput id="email" v-model="email" class="w-full" type="email" placeholder="E-mailadres" />
          </UFormField>
        </div>

        <div class="mb-4">
          <UFormField name="password" label="Wachtwoord" size="xl">
            <UInput id="password" v-model="password" class="w-full" type="password" placeholder="Wachtwoord" />
          </UFormField>
        </div>

        <div v-if="error" class="text-red-600">{{ error }}</div>
      </div>
      <template #footer>
        <div class="flex justify-end">
          <UButton
            class="mr-auto"
            label="Al een account?"
            color="primary"
            variant="ghost"
            type="button"
            :to="{ name: 'login' }"
          />
          <UButton class="justify-end" type="submit" :loading="loading" loading-icon="i-lucide-loader" label="Registreren" color="primary" />
        </div>
      </template>
    </UCard>
  </UForm>
</template>
