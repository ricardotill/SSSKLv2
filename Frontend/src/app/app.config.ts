import { ApplicationConfig, ErrorHandler, provideAppInitializer, provideBrowserGlobalErrorListeners, inject, LOCALE_ID, isDevMode } from '@angular/core';
import { provideServiceWorker } from '@angular/service-worker';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { registerLocaleData } from '@angular/common';
import localeNl from '@angular/common/locales/nl';

registerLocaleData(localeNl);
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { providePrimeNG } from 'primeng/config';
import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura-compat';
import { MessageService, ConfirmationService } from 'primeng/api';

import { provideHttpClient, withFetch, withInterceptors, withXsrfConfiguration } from '@angular/common/http';
import { authInterceptor } from './core/auth/auth.interceptor';
import { apiBaseInterceptor } from './core/interceptors/api-base.interceptor';
import { environment } from '../environments/environment';
import { PwaZoomLockService } from './core/services/pwa-zoom-lock.service';
import { isEmbedded } from './core/services/embedded.service';
import * as Sentry from '@sentry/angular';

const MyPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{emerald.50}',
      100: '{emerald.100}',
      200: '{emerald.200}',
      300: '{emerald.300}',
      400: '{emerald.400}',
      500: '{emerald.500}',
      600: '{emerald.600}',
      700: '{emerald.700}',
      800: '{emerald.800}',
      900: '{emerald.900}',
      950: '{emerald.950}'
    }
  }
});

export const appConfig: ApplicationConfig = {
  providers: [
    provideAnimationsAsync(),
    provideBrowserGlobalErrorListeners(),
    // Report uncaught errors to Sentry (still logged to the console), and trace route changes.
    { provide: ErrorHandler, useValue: Sentry.createErrorHandler() },
    provideAppInitializer(() => { inject(Sentry.TraceService); }),
    provideRouter(routes),
    provideHttpClient(
      withFetch(),
      withInterceptors([apiBaseInterceptor, authInterceptor]),
      withXsrfConfiguration({
        cookieName: 'XSRF-TOKEN',
        headerName: 'X-XSRF-TOKEN'
      })
    ),
    providePrimeNG({
      license: environment.primeUiLicenseKey || undefined,
      theme: {
        preset: MyPreset,
        options: {
          darkModeSelector: '.dark'
        }
      }
    }),
    { provide: MessageService, useClass: MessageService },
    { provide: ConfirmationService, useClass: ConfirmationService },
    { provide: LOCALE_ID, useValue: 'nl-NL' },
    // Installed app only: no pinch/double-tap/keyboard zoom, like a native app
    provideAppInitializer(() => inject(PwaZoomLockService).init()),
    // Not in the mobile app's WebView: no offline caching or update reloads there.
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isEmbedded(),
      registrationStrategy: 'registerImmediately'
    })
  ]
};
