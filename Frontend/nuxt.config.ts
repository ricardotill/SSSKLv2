// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({

  modules: [
    '@nuxt/eslint',
    '@nuxt/fonts',
    '@nuxt/icon',
    '@nuxt/image',
    '@nuxt/scripts',
    '@nuxt/test-utils',
    '@pinia/nuxt',
    '@nuxt/ui',
    'nuxt-open-fetch',
  ],
  devtools: { enabled: true },
  css: ['~/assets/css/main.css'],
  runtimeConfig: {
    public: {
      // Use the HTTPS port configured in launchSettings (https profile) so the dev frontend talks to the running HTTPS server.
      apiBase: 'https://localhost:7193',
    },
  },
  compatibilityDate: '2025-07-15',
  openFetch: {
    clients: {
      sssklv2: {}, // The key should be specified as it is used to generate the client
    },
  },
})
