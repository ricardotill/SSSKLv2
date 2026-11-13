# Mobile app releases

How the MAUI app (`Mobile/`) gets to TestFlight, the App Store and Google Play. The design and the decisions behind it are in [mobile-release-plan.md](mobile-release-plan.md).

## How a release works

`.github/workflows/mobile-release.yml` runs for every release tag `vX.Y.Z` on master, the same tag that deploys the API and SPA:

```
build-android ──► android-internal (approve) ──► android-production (approve, stable tags only)
build-ios     ──► ios-testflight   (approve) ──► ios-app-store      (approve, stable tags only)
```

1. **Build** (no approval): each app is tested (Core), built, signed and stored as a workflow artifact (`android-aab`, `ios-ipa`). Symbols go to Sentry (`ssskl-mobile`) and to the stores: the `.ipa` carries a `Symbols/` folder for App Store Connect (the build fails without it), and the Android job zips the unstripped Native AOT library (`native-debug-symbols.zip`, uploaded with the bundle as native debug symbols); the R8 mapping for Java frames is already inside the `.aab`.
2. **Testing** (approve in the run): the `.aab` goes to the Play **internal testing** track; the `.ipa` goes to **TestFlight** for internal testers.
3. **Production** (approve after testing): the same build is promoted from internal testing to **production** (Google reviews it), and attached to a new App Store version that is **submitted for App Review**. After Apple approves it, press *Release* in App Store Connect.

A pre-release tag (`v1.5.0-rc.1`) stops after step 2. If a release needs no new app, reject its pending approvals; a pending approval expires after 30 days.

**Versions**: the display version is the tag's numbers (`1.5.0`, also for `1.5.0-rc.1`). The build number (iOS `CFBundleVersion`, Android `versionCode`) is the workflow's run number + `BUILD_NUMBER_OFFSET` (1000). It is the same on both platforms and shows in the job summaries.

**Retry**: re-run the failed job from the run page; it reuses the artifact and build number. To start over for a tag: Actions → Mobile release → Run workflow → pick the tag. That gives a new build number.

## One-time setup

### Apple
1. Enrol **Scouting Wilo** in the Apple Developer Program as an organisation (needs a D-U-N-S number; €99/year).
2. Certificates, Identifiers & Profiles:
   - Register the App ID `nl.scoutingwilo.ssskl` (no extra capabilities).
   - Create an **App Store** provisioning profile for it. CI downloads it, so it never goes in a secret.
   - Create an **Apple Distribution** certificate and export it from Keychain Access as a `.p12` with a password.
3. App Store Connect → Users and Access → Integrations: create an **API key** with role *App Manager* and download the `.p8`. Note its Key ID and the Issuer ID.
4. App Store Connect → Apps → **New app**: name SSSKL, bundle ID `nl.scoutingwilo.ssskl`, primary language Dutch.
5. Fill in the App Store listing:
   - App Privacy: match `Mobile/SSSKL.Mobile/Platforms/iOS/PrivacyInfo.xcprivacy`.
   - Age rating, privacy policy URL and support URL.
   - iPhone 6.9" screenshots.
   - App Review information: a demo account (User role) and a note that the saldo pays for drinks at the club, outside the app.
6. Request **Unlisted App Distribution** (Apple's request form), so the app is only reachable by its link.
7. TestFlight: create an internal testing group with the testers.

### Google
1. Create a Google Play Console account for **Scouting Wilo** as an organisation (needs a D-U-N-S number; $25 once).
2. Create the app. Its package name is set by the first upload (`nl.scoutingwilo.ssskl`). Keep **Play App Signing** on.
3. Create the **upload keystore** once and keep it, and its passwords, in a password manager:
   ```bash
   keytool -genkeypair -v -keystore ssskl-upload.keystore -alias ssskl-upload \
     -keyalg RSA -keysize 4096 -validity 10000
   ```
4. Google Cloud: create a **service account** and a JSON key. In Play Console → Users and permissions, invite its e-mail with *Release apps to testing tracks* and *Release to production* for this app.
5. Internal testing: add the testers (e-mail list or Google group).
6. Fill in the store listing, Data safety, content rating, target audience and **app access** sections, using the same demo account.
7. **First upload by hand.** The Play API can't upload an app's first build. Push a pre-release tag (or run the workflow on one), download the `android-aab` artifact from the run, and upload it to internal testing in Play Console. From then on the workflow does it.

### GitHub
Settings → Environments. Restrict every environment to tags matching `v*` (*Deployment branches and tags → Selected*). Add **Ricardo** as required reviewer to the four store environments, and leave *Prevent self-review* off.

| Environment | Reviewers | Secrets |
|---|---|---|
| `mobile-signing` | none | `ANDROID_KEYSTORE_BASE64` (`base64 -i ssskl-upload.keystore`), `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS` (`ssskl-upload`), `ANDROID_KEY_PASSWORD`, `IOS_DIST_CERT_P12_BASE64` (`base64 -i dist.p12`), `IOS_DIST_CERT_PASSWORD`, `ASC_KEY_ID`, `ASC_ISSUER_ID`, `ASC_KEY_P8` (the `.p8` file contents) |
| `ios-testflight` | Ricardo | `ASC_KEY_ID`, `ASC_ISSUER_ID`, `ASC_KEY_P8` |
| `ios-app-store` | Ricardo | `ASC_KEY_ID`, `ASC_ISSUER_ID`, `ASC_KEY_P8` |
| `android-internal` | Ricardo | `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` (the whole JSON file) |
| `android-production` | Ricardo | `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` |

Optional repository variable: `ANDROID_PRODUCTION_ROLLOUT`, for example `0.2` for a staged rollout to 20% of users. Without it, the release goes to everyone. `SENTRY_AUTH_TOKEN` is already a repository secret.

## Store screenshots

`Mobile/screenshots/screenshots.sh` generates the Android phone screenshots (1080×2160, the most Google Play allows is 2:1) into `Mobile/screenshots/output/<theme>/`:

```bash
brew install mobile-dev-inc/tap/maestro          # once
Mobile/screenshots/screenshots.sh --theme both   # light|dark|both; --avd NAME, --skip-build, --output DIR
```

It starts the API in the E2E environment with `E2E:DemoData=true` (fictional members, products, orders, events, quotes and achievements from `Backend/SSSKL.Web/Registrations/DemoDataSeeder.cs`, in a throwaway SQLite database) on :5253, and the SPA (`start:mobile`) on :4300. It then boots the emulator (or uses the connected device), installs a Debug build and runs the Maestro flow `Mobile/screenshots/flow.yaml`. That flow signs in as `lotte` and visits the screens. Afterwards the script restores the device's screen size, status bar and dark mode. Port 4300 must be free. Logs are in `output/logs/`. Change the screens in `flow.yaml`; Google Play takes at most 8 phone screenshots.

## Maintenance

- **Xcode**: .NET for iOS needs one exact Xcode. When the iOS workload moves on, update `XCODE_APP` in `mobile-release.yml` and the *Select Xcode* step in `mobile.yml`. Both run on the `xcode-27` runner image, which has no .NET 11, so `setup-dotnet` installs it.
- **Distribution certificate**: it expires after a year. Create a new one and replace `IOS_DIST_CERT_P12_BASE64` and `IOS_DIST_CERT_PASSWORD`. The provisioning profile has to be regenerated for the new certificate (in the developer portal); CI then downloads it.
- **Upload keystore lost**: ask Google to reset the upload key in Play Console (App integrity). The app signing key stays with Google.
- **Android Native AOT**: if a Release build misbehaves only with AOT, add `-p:SssklAndroidAot=false` to the Android *Publish* step.
- **Target API level**: Google Play raises the required target SDK every year (August). A newer MAUI version normally raises it too; check the Play Console warnings.
- **fastlane**: pinned in `Mobile/Gemfile.lock`. Update it with `cd Mobile && bundle update fastlane`. The lanes are in `Mobile/fastlane/Fastfile`.
