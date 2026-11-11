// Runs after `npm run build` (postbuild). With SENTRY_AUTH_TOKEN set (Static Web Apps deploy), injects Debug
// IDs into the built JS, uploads the hidden source maps to Sentry (project ssskl-frontend) and then deletes
// the .map files so they are never deployed. Without the token (local builds, CI) it only deletes the maps.
//
// A Sentry failure (outage, expired token) never fails the build: it prints a GitHub Actions warning and the
// build stays deployable. Injecting rewrites JS files after Angular hashed them into ngsw.json, so whenever
// inject ran (even if it failed halfway) the hashes in ngsw.json are recomputed from the files on disk;
// otherwise the service worker rejects the new version and PWA updates never install.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { execFileSync } = require('child_process');
const { sentryRelease } = require('./sentry-release');

const dist = path.join(__dirname, 'dist', 'SSSKL', 'browser');
const cli = require.resolve('@sentry/cli/bin/sentry-cli');
const serviceWorkerScripts = ['ngsw-worker.js', 'safety-worker.js', 'worker-basic.min.js'];

function sentryCli(...args) {
  execFileSync(process.execPath, [cli, ...args], {
    stdio: 'inherit',
    env: { ...process.env, SENTRY_ORG: 'ricardo-tillemans', SENTRY_PROJECT: 'ssskl-frontend' },
  });
}

function warn(message) {
  // `::warning::` shows up as an annotation on the GitHub Actions run.
  console.warn(`::warning::${message}`);
}

function removeSourceMaps() {
  let removed = 0;
  for (const file of fs.readdirSync(dist, { recursive: true })) {
    if (String(file).endsWith('.map')) {
      fs.rmSync(path.join(dist, String(file)));
      removed++;
    }
  }
  console.log(`Removed ${removed} source map file(s) from the build output.`);
}

/**
 * Recomputes the SHA-1 hashes in ngsw.json (the same hash Angular's ngsw-config generator uses) from the
 * files as they are now. Rehashing in place keeps the manifest identical to the one the Angular CLI wrote
 * otherwise: re-running the generator on the output folder would also list ngsw-worker.js, safety-worker.js
 * and worker-basic.min.js, which the CLI copies only after it generated the manifest.
 */
function rehashServiceWorkerManifest() {
  const manifestPath = path.join(dist, 'ngsw.json');
  if (!fs.existsSync(manifestPath)) {
    return;
  }
  const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
  let changed = 0;
  for (const url of Object.keys(manifest.hashTable ?? {})) {
    const file = path.join(dist, ...url.split('/').filter(Boolean));
    if (!fs.existsSync(file)) {
      continue;
    }
    const hash = crypto.createHash('sha1').update(fs.readFileSync(file)).digest('hex');
    if (manifest.hashTable[url] !== hash) {
      manifest.hashTable[url] = hash;
      changed++;
    }
  }
  fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2));
  console.log(`Updated ${changed} hash(es) in ngsw.json.`);
}

if (process.env.SENTRY_AUTH_TOKEN) {
  console.log(`Uploading source maps to Sentry for ${sentryRelease}`);
  // Inject first (Debug IDs tie each minified file to its map), then upload. If the upload fails the
  // injected files are still valid JS (the Debug ID snippet is harmless), so they are kept.
  try {
    // The service worker scripts have no source maps and are not ours: leave them untouched.
    sentryCli('sourcemaps', 'inject', ...serviceWorkerScripts.flatMap(file => ['--ignore', file]), dist);
    sentryCli('sourcemaps', 'upload', '--release', sentryRelease, dist);
  } catch (error) {
    warn(`Sentry source map upload failed, deploying without readable stack traces: ${error.message}`);
  }
  rehashServiceWorkerManifest();
} else {
  console.warn('SENTRY_AUTH_TOKEN not set: source maps are not uploaded to Sentry.');
}

removeSourceMaps();
