// @ts-check
import { createConfigForNuxt } from '@nuxt/eslint-config/flat'

export default createConfigForNuxt({
  features: {
    stylistic: {
      semi: false,
      indent: 2, // 4 or 'tab'
      quotes: 'single',
    },
  },
})
