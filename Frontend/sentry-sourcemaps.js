// Runs after `npm run build` (postbuild). With SENTRY_AUTH_TOKEN set (Static Web Apps deploy), injects Debug
// IDs into the built JS, uploads the hidden source maps to Sentry (project ssskl-frontend) and then deletes
// the .map files so they are never deployed. Without the token (local builds, CI) it only deletes the maps.
const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');
const { sentryRelease } = require('./sentry-release');

const dist = path.join(__dirname, 'dist', 'SSSKL', 'browser');
const cli = require.resolve('@sentry/cli/bin/sentry-cli');

function sentryCli(...args) {
  execFileSync(process.execPath, [cli, ...args], {
    stdio: 'inherit',
    env: { ...process.env, SENTRY_ORG: 'ricardo-tillemans', SENTRY_PROJECT: 'ssskl-frontend' },
  });
}

if (process.env.SENTRY_AUTH_TOKEN) {
  console.log(`Uploading source maps to Sentry for ${sentryRelease}`);
  // Inject first (Debug IDs tie each minified file to its map), then upload. Fails the build on error,
  // so a deploy never ships with unreadable stack traces.
  sentryCli('sourcemaps', 'inject', dist);
  sentryCli('sourcemaps', 'upload', '--release', sentryRelease, dist);
} else {
  console.warn('SENTRY_AUTH_TOKEN not set: source maps are not uploaded to Sentry.');
}

let removed = 0;
for (const file of fs.readdirSync(dist, { recursive: true })) {
  if (String(file).endsWith('.map')) {
    fs.rmSync(path.join(dist, String(file)));
    removed++;
  }
}
console.log(`Removed ${removed} source map file(s) from the build output.`);
