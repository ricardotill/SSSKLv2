export const environment = {
  apiUrl: typeof CUSTOM_API_URL !== 'undefined' ? CUSTOM_API_URL : '',
  primeUiLicenseKey: typeof PRIMEUI_LICENSE_KEY !== 'undefined' ? PRIMEUI_LICENSE_KEY : '',
  // Sentry project ssskl-frontend. The DSN is a public client key, safe to ship in the bundle.
  sentryDsn: 'https://57bdf0a5bc34d9fc50dbbd0b98120344@o4512396434341888.ingest.de.sentry.io/4512396459442256',
  // "ssskl-frontend@<package.json version>", set by ng-env.js; matches the uploaded source maps.
  sentryRelease: typeof SENTRY_RELEASE !== 'undefined' ? SENTRY_RELEASE : undefined
};
