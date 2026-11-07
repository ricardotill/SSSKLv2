import { defineConfig, devices } from '@playwright/test';
import { PASSWORD } from './e2e/support/users';

// The E2E suite runs the real API (SSSKL.Web, ASPNETCORE_ENVIRONMENT=E2E) on a fresh, seeded SQLite
// database and serves the SPA with the dev server, which proxies /api and /hubs to that API.
const CI = !!process.env['CI'];
const API_URL = 'http://localhost:5252';
const WEB_URL = 'http://localhost:4300';

export default defineConfig({
  testDir: './e2e',
  testMatch: '**/*.e2e.ts',
  outputDir: './test-results',
  // All specs share one seeded database and assert saldo changes, so run them one at a time.
  fullyParallel: false,
  workers: 1,
  retries: CI ? 2 : 0,
  forbidOnly: CI,
  timeout: 30_000,
  expect: { timeout: 10_000 },
  reporter: CI ? [['github'], ['html', { open: 'never' }]] : [['list'], ['html', { open: 'on-failure' }]],
  use: {
    baseURL: WEB_URL,
    locale: 'nl-NL',
    timezoneId: 'Europe/Amsterdam',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    serviceWorkers: 'block',
  },
  projects: [
    { name: 'setup', testMatch: /.*\.setup\.ts/ },
    { name: 'chromium', use: { ...devices['Desktop Chrome'] }, dependencies: ['setup'] },
  ],
  webServer: [
    {
      command: `dotnet run --project ../SSSKL.Web/SSSKL.Web.csproj --launch-profile e2e${CI ? ' --no-build -c Release' : ''}`,
      url: `${API_URL}/healthz/live`,
      reuseExistingServer: !CI,
      timeout: 240_000,
      stdout: 'pipe',
      // E2ESeeder gives every seeded user this password.
      env: { E2E__UserPassword: PASSWORD },
    },
    {
      // Calls ng-env.js directly (not `npm start`) to skip the HTTPS certificate setup.
      command: 'node ng-env.js serve --port 4300 --ssl=false',
      url: WEB_URL,
      reuseExistingServer: !CI,
      timeout: 240_000,
      env: { services__ssskl__http__0: API_URL, services__ssskl__https__0: '' },
    },
  ],
});
