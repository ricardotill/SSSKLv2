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

// The Turnstile sitekey is public; it is passed the same way so each environment can use its own widget.
const turnstileSiteKey = process.env.TURNSTILE_SITE_KEY;
if (turnstileSiteKey) {
  args.push(`--define=TURNSTILE_SITE_KEY=${JSON.stringify(turnstileSiteKey)}`);
}

const ng = spawn(process.execPath, [require.resolve('@angular/cli/bin/ng.js'), ...args], { stdio: 'inherit' });
ng.on('exit', (code) => process.exit(code ?? 1));
