// Sentry release of the SPA: "ssskl-frontend@<Version>", with the version from Directory.Build.props, the
// single app version shared with the API (SSSKL.Web@<Version>) and the mobile app (SSSKL.Mobile@<Version>).
const fs = require('fs');
const path = require('path');

function appVersion() {
  const props = fs.readFileSync(path.join(__dirname, '..', 'Directory.Build.props'), 'utf8');
  const match = props.match(/<Version>([^<]+)<\/Version>/);
  if (!match) {
    throw new Error('No <Version> in Directory.Build.props');
  }
  return match[1].trim();
}

const sentryRelease = process.env.SENTRY_RELEASE || `ssskl-frontend@${appVersion()}`;

module.exports = { sentryRelease };
