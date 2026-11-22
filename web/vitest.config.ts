import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vitest/config'

// Testy jednostkowe czystych funkcji (bez przeglądarki). Testy e2e (Playwright) są w e2e/ i mają własną konfigurację.
export default defineConfig({
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  test: {
    include: ['src/**/*.test.ts'],
    environment: 'node',
  },
})
