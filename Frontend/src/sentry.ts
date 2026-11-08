import { isDevMode } from '@angular/core';
import * as Sentry from '@sentry/angular';
import { environment } from './environments/environment';
import { isEmbedded } from './app/core/services/embedded.service';

/**
 * Errors and traces to Sentry (project ssskl-frontend). Runs before bootstrap so startup errors are
 * captured too. Not in automated browsers (Playwright E2E, unit tests), so test runs never report.
 */
export function initSentry(): void {
  Sentry.init({
    dsn: environment.sentryDsn,
    enabled: !!environment.sentryDsn && !navigator.webdriver,
    environment: isDevMode() ? 'development' : 'production',
    release: environment.sentryRelease,
    integrations: [Sentry.browserTracingIntegration()],
    tracesSampleRate: isDevMode() ? 1.0 : 0.2,
    // The API is same-origin (relative /api URLs), so traces continue into the ssskl-api project.
    tracePropagationTargets: [/^\/api\//, /^\/hubs\//],
    initialScope: { tags: { embedded: isEmbedded() } },
  });
}
