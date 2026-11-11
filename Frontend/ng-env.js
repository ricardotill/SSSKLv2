// Runs the Angular CLI with secrets from the environment (or a gitignored .env) injected as build-time defines.
const fs = require('fs');
const path = require('path');
const { spawn } = require('child_process');

const envFile = path.join(__dirname, '.env');
if (fs.existsSync(envFile)) {
  process.loadEnvFile(envFile);
}

const args = process.argv.slice(2);
const licenseKey = process.env.PRIMEUI_LICENSE_KEY;
if (licenseKey) {
  args.push(`--define=PRIMEUI_LICENSE_KEY=${JSON.stringify(licenseKey)}`);
} else {
  console.warn('PRIMEUI_LICENSE_KEY not set; PrimeUI will show its license banner.');
}

// App version (the release tag, see sentry-release.js) and the Sentry release name; the release must match
// the one the source maps are uploaded for (sentry-sourcemaps.js).
const { appVersion, sentryRelease } = require('./sentry-release');
args.push(`--define=APP_VERSION=${JSON.stringify(appVersion)}`);
args.push(`--define=SENTRY_RELEASE=${JSON.stringify(sentryRelease)}`);

const ng = spawn(process.execPath, [require.resolve('@angular/cli/bin/ng.js'), ...args], { stdio: 'inherit' });
ng.on('exit', (code) => process.exit(code ?? 1));
