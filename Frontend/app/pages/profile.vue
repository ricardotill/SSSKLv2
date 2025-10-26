<script setup lang="ts">
import { ref, computed, reactive, watch } from 'vue'
import type { NavigationMenuItem } from '@nuxt/ui'
import { useAuth } from '~/composables/useAuth'

definePageMeta({ layout: 'dashboard' })

const items: NavigationMenuItem[][] = [
  [
    {
      label: 'Profiel',
      icon: 'i-lucide-user',
      active: true,
      value: '0',
    },
    {
      label: 'E-mailadres',
      icon: 'i-lucide-mail',
      value: '1',
    },
    {
      label: 'Wachtwoord',
      icon: 'i-lucide-key-round',
      value: '2',
    },
    {
      label: 'Twee-factor authenticatie',
      icon: 'i-lucide-rectangle-ellipsis',
      value: '3',
    },
    {
      label: 'Persoonlijke data',
      icon: 'i-lucide-id-card',
      value: '4',
    },
  ],
  [],
]

const active = ref('0')
const selectedTab = computed(() => Number(active.value) || 0)

// auth
const { user, updateMe } = useAuth()
const saving = ref(false)

const form = reactive({ userName: '', name: '', surname: '', phoneNumber: '' })
const serverErrors = reactive<Record<string, string[]>>({})
const toast = useToast()

function firstErrorFor(field: string) {
  // Try several common key styles returned by backend
  if (serverErrors[field] && serverErrors[field].length) return serverErrors[field][0]
  const lower = field.charAt(0).toLowerCase() + field.slice(1)
  if (serverErrors[lower] && serverErrors[lower].length) return serverErrors[lower][0]
  const upper = field.charAt(0).toUpperCase() + field.slice(1)
  if (serverErrors[upper] && serverErrors[upper].length) return serverErrors[upper][0]
  return null
}

watch(user, (u) => {
  if (u) {
    form.userName = u.userName ?? ''
    form.name = u.name ?? ''
    form.surname = u.surname ?? ''
    form.phoneNumber = u.phoneNumber ?? ''
  }
}, { immediate: true })

async function onSubmitProfile() {
  saving.value = true
  try {
    // clear previous errors
    for (const k of Object.keys(serverErrors)) {
      serverErrors[k] = []
    }

    // send only fields that can be updated (userName is read-only)
    await updateMe({
      name: form.name,
      surname: form.surname,
      phoneNumber: form.phoneNumber,
    })
    toast.add({ title: 'Succes', color: 'success' })
  }
  catch (err: unknown) {
    // handle validation errors attached by useAuth.updateMe
    const maybe = err as { validationErrors?: Record<string, string[]>, message?: string }
    if (maybe?.validationErrors) {
      // copy validation errors into reactive serverErrors
      for (const [k, v] of Object.entries(maybe.validationErrors)) {
        serverErrors[k] = v.slice()
      }
      toast.add({ title: 'Controleer invoer', color: 'error' })
    }
    else {
      toast.add({ title: (maybe && (maybe as { message?: string }).message) || 'Opslaan mislukt', color: 'error' })
    }
  }
  finally {
    saving.value = false
  }
}

const itemsWithHandlers = computed(() => {
  return items.map(group =>
    group.map(item => ({
      ...item,
      active: item.value === undefined ? false : String(item.value) === active.value,
      onSelect: () => {
        if (item.value !== undefined)
          active.value = String(item.value)
      },
    })),
  )
})
</script>

<template>
  <div>
    <UDashboardToolbar>
      <UNavigationMenu
        v-model="active"
        orientation="horizontal"
        highlight
        highlight-color="primary"
        :items="itemsWithHandlers"
      />
    </UDashboardToolbar>
    <UContainer>
      <div class="mt-6 max-w-4xl mx-auto">
        <UForm
          v-if="selectedTab === 0"
          @submit.prevent="onSubmitProfile"
        >
          <UCard
            class="p-4"
            variant="subtle"
          >
            <template #header>
              <!-- Profiel panel: editable form -->
              <h2 class="text-xl font-semibold mb-2">
                Profiel
              </h2>
              <p class="text-sm text-muted-foreground">
                Hier kun je je profielgegevens bekijken en bijwerken.
              </p>
            </template>

            <div class="mb-4">
              <UFormField
                name="userName"
                label="Gebruikersnaam"
                size="xl"
              >
                <UInput
                  id="userName"
                  v-model="form.userName"
                  class="w-full"
                  type="text"
                  placeholder="Gebruikersnaam"
                  disabled
                />
              </UFormField>
            </div>

            <div class="mb-4">
              <UFormField
                name="name"
                label="Voornaam"
                size="xl"
              >
                <UInput
                  id="name"
                  v-model="form.name"
                  class="w-full"
                  type="text"
                  placeholder="Voornaam"
                />
                <p
                  v-if="firstErrorFor('name')"
                  class="text-sm text-destructive mt-1"
                >
                  {{ firstErrorFor('name') }}
                </p>
              </UFormField>
            </div>

            <div class="mb-4">
              <UFormField
                name="surname"
                label="Achternaam"
                size="xl"
              >
                <UInput
                  id="surname"
                  v-model="form.surname"
                  class="w-full"
                  type="text"
                  placeholder="Achternaam"
                />
                <p
                  v-if="firstErrorFor('surname')"
                  class="text-sm text-destructive mt-1"
                >
                  {{ firstErrorFor('surname') }}
                </p>
              </UFormField>
            </div>

            <div class="mb-4">
              <UFormField
                name="phoneNumber"
                label="Telefoonnummer"
                size="xl"
              >
                <UInput
                  id="phoneNumber"
                  v-model="form.phoneNumber"
                  class="w-full"
                  type="tel"
                  placeholder="Telefoonnummer"
                />
                <p
                  v-if="firstErrorFor('phoneNumber')"
                  class="text-sm text-destructive mt-1"
                >
                  {{ firstErrorFor('phoneNumber') }}
                </p>
              </UFormField>
            </div>

            <template #footer>
              <div class="flex justify-end">
                <UButton
                  class="justify-end"
                  type="submit"
                  :loading="saving"
                  loading-icon="i-lucide-loader"
                  label="Opslaan"
                  color="primary"
                />
              </div>
            </template>
          </UCard>
        </UForm>

        <div v-else-if="selectedTab === 1">
          <!-- E-mailadres panel -->
          <h2 class="text-xl font-semibold mb-2">
            E-mailadres
          </h2>
          <p class="text-sm text-muted-foreground">
            Wijzig je e-mailadres.
          </p>
        </div>

        <div v-else-if="selectedTab === 2">
          <!-- Wachtwoord panel -->
          <h2 class="text-xl font-semibold mb-2">
            Wachtwoord
          </h2>
          <p class="text-sm text-muted-foreground">
            Wijzig je wachtwoord.
          </p>
        </div>

        <div v-else-if="selectedTab === 3">
          <!-- Twee-factor authenticatie panel -->
          <h2 class="text-xl font-semibold mb-2">
            Twee-factor authenticatie
          </h2>
          <p class="text-sm text-muted-foreground">
            Beheer je twee-factor authenticatie instellingen.
          </p>
        </div>

        <div v-else-if="selectedTab === 4">
          <!-- Persoonlijke data panel -->
          <h2 class="text-xl font-semibold mb-2">
            Persoonlijke data
          </h2>
          <p class="text-sm text-muted-foreground">
            Bekijk en exporteer je persoonlijke data.
          </p>
        </div>
      </div>
    </UContainer>
  </div>
</template>
