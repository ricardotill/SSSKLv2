// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },

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
  css: ['~/assets/css/main.css'],
  openFetch: {
    clients: {
      sssklv2: {} // The key should be specified as it is used to generate the client
    }
  }
  ,
  runtimeConfig: {
    public: {
      // Use the HTTPS port configured in launchSettings (https profile) so the dev frontend talks to the running HTTPS server.
      apiBase: 'https://localhost:7193'
    }
  }
})