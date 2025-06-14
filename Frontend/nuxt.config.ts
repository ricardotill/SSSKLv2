import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

const sssklTheme = definePreset(Aura, {
  semantic: {
      primary: {
          50: '{rose.50}',
          100: '{rose.100}',
          200: '{rose.200}',
          300: '{rose.300}',
          400: '{rose.400}',
          500: '{rose.500}',
          600: '{rose.600}',
          700: '{rose.700}',
          800: '{rose.800}',
          900: '{rose.900}',
          950: '{rose.950}'
      }
  }
});

// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2024-11-01',
  devtools: { enabled: true },
  modules: ['@nuxt/test-utils', '@nuxt/scripts', '@nuxt/fonts', '@primevue/nuxt-module'],
  primevue: {
    options: {
      theme: {
        preset: sssklTheme,
        options: {
          prefix: 'p',
          darkModeSelector: '.mk-app-dark',
        }
      }
    }
  }
})