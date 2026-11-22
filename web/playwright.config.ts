import { defineConfig, devices } from '@playwright/test'

/**
 * Testy e2e: API w trybie bez bazy (bez ConnectionStrings__Default używa bazy w pamięci) i Vite z proxy do API.
 * Porty 5180/5181, żeby nie kolidować z serwerami deweloperskimi (5080/5173).
 */
const API_URL = 'http://localhost:5180'
const WEB_URL = 'http://localhost:5181'
const ci = Boolean(process.env.CI)

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: ci,
  retries: ci ? 1 : 0,
  // API w pamięci jest wspólne dla wszystkich testów; kilka wątków w zupełności wystarcza.
  workers: ci ? 2 : 3,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: ci ? [['list'], ['html', { open: 'never' }]] : [['list']],
  use: {
    baseURL: WEB_URL,
    locale: 'pl-PL',
    timezoneId: 'Europe/Warsaw',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: `dotnet run --no-launch-profile --urls ${API_URL} --project ../api/Api`,
      url: `${API_URL}/health`,
      env: { ADMIN_PASSWORD: 'e2e', ConnectionStrings__Default: '' },
      reuseExistingServer: !ci,
      timeout: 240_000,
      stdout: 'ignore',
      stderr: 'pipe',
    },
    {
      command: 'npx vite --port 5181 --strictPort',
      url: WEB_URL,
      env: { VITE_API_PROXY: API_URL },
      reuseExistingServer: !ci,
      timeout: 120_000,
    },
  ],
})
