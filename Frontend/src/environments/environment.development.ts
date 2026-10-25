export const environment = {
  apiUrl: typeof CUSTOM_API_URL !== 'undefined' ? CUSTOM_API_URL : '',
  primeUiLicenseKey: typeof PRIMEUI_LICENSE_KEY !== 'undefined' ? PRIMEUI_LICENSE_KEY : '',
  // Public Cloudflare Turnstile sitekey; empty disables the widget (the API then must not require it either)
  turnstileSiteKey: typeof TURNSTILE_SITE_KEY !== 'undefined' ? TURNSTILE_SITE_KEY : ''
};
