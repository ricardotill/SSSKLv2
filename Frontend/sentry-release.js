// App version and Sentry release of the SPA. The version is the release tag (v1.2.3 → 1.2.3), passed by CI as
// APP_VERSION (.github/workflows/version.yml); the API (SSSKL.Web@<Version>) and the mobile app
// (SSSKL.Mobile@<Version>) get the same one as -p:Version. A local build falls back to the default
// <Version> in Directory.Build.props (0.0.0).
const fs = require('fs');
const path = require('path');

function appVersion() {
  if (process.env.APP_VERSION) {
    return process.env.APP_VERSION.trim();
  }
  const props = fs.readFileSync(path.join(__dirname, '..', 'Directory.Build.props'), 'utf8');
  const match = props.match(/<Version[^>]*>([^<]+)<\/Version>/);
  if (!match) {
    throw new Error('No <Version> in Directory.Build.props');
  }
  return match[1].trim();
}

const version = appVersion();
const sentryRelease = process.env.SENTRY_RELEASE || `ssskl-frontend@${version}`;

module.exports = { appVersion: version, sentryRelease };
