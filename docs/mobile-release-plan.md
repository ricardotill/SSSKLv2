# Mobile app: release pipeline and polish plan

Status: Ricardo approved every recommendation in 1.2 on 2026-11-12, including organisation accounts for Scouting Wilo. Neither store account exists yet. The repo side (1.6 and the workflow in 1.5) is implemented; [mobile-release.md](mobile-release.md) is the runbook for the setup and releases. Written 2026-11-12 against `master` at `e2ced7e`.

This plan covers two things:

1. **CI/CD** for the .NET MAUI app (`Mobile/`): signed iOS and Android builds that go through two approvable stages per platform, first **TestFlight / Google Play internal testing**, then **App Store / Google Play production**.
2. **Visual polish** still open in the app after the polish pass of PR #123 (native "Meer" and settings pages, hidden SPA chrome, WebView fade-in, Aura colours, Tabler icons).

---

## Part 1: CI/CD

### 1.1 What exists today

- `.github/workflows/mobile.yml` runs by hand only: Core tests plus an unsigned Android build and an unsigned iOS simulator build. Nothing is signed or uploaded.
- `SSSKL.Mobile.csproj`: `ApplicationId` `nl.scoutingwilo.ssskl`, `ApplicationDisplayVersion = $(Version)`, `ApplicationVersion = 1` (hard-coded), Native AOT in Release, Sentry symbol upload when `SENTRY_AUTH_TOKEN` is set.
- Versioning: the git tag `vX.Y.Z` on master is the single source (`version.yml`), shared by API, SPA and Docker images.
- The iOS CI build failed on `macos-latest` (macOS 26, no Xcode 27.2). The `xcode-27` runner image has Xcode 27.2 but no .NET 11, so the iOS jobs run there and install .NET 11 with `setup-dotnet`.
- The repo is public, so GitHub environments with required reviewers are available on the free plan.

### 1.2 Decisions (recommendation first)

| # | Decision | Recommendation | Why |
|---|----------|----------------|-----|
| D1 | Which tag ships the app | **Same `vX.Y.Z` tag as the backend.** Pre-release tags (`v1.5.0-rc.1`) go only as far as TestFlight / internal testing. | One version across API, SPA and app; Sentry releases already use it (`SSSKL.Mobile@<v>`). A release that doesn't need a new app just gets its pending approvals rejected. Alternative: a separate `mobile-vX.Y.Z` tag if app releases should be fully decoupled. |
| D2 | Upload tooling | **fastlane** (`pilot`, `deliver`, `supply`), pinned with a `Gemfile.lock` in `Mobile/fastlane/`. | It is the only tool that covers all four steps, including App Store submission for review and Play track promotion, with API keys and no passwords. The `dotnet` CLI only builds and signs. |
| D3 | Store accounts: personal or organisation | **Organisation accounts under Scouting Wilo** if the club has (or can get) a free D-U-N-S number; otherwise Ricardo's personal accounts. | The store shows the account name as the seller. A new **personal** Google Play account must also run a closed test with at least 12 testers for 14 days before it can publish to production; organisation accounts skip that. |
| D4 | Public or unlisted listing | **iOS: Unlisted App Distribution. Android: public listing** (or keep it on a closed track). | The app is only useful to club members. Apple's unlisted distribution keeps it out of search and only reachable by link, and reduces "minimum functionality" review friction. Ask for it via Apple's request form once the app record exists. |
| D5 | iPad support | **iPhone only for the first release** (`UIDeviceFamily` = 1). | `Info.plist` currently declares iPad too, which makes App Store Connect require iPad screenshots and App Review test it on iPad. Layouts are phone-first (no max width). Add iPad later with the polish item P15. |
| D6 | Native AOT on Android | **Keep it, with an escape hatch** (`-p:SssklAndroidAot=false`) and decide after internal testing. | The Android SDK still marks AOT experimental (XA1040). A crash that only shows in AOT builds should not block a release. |
| D7 | Build number | **`github.run_number + 1000`** of the release workflow, the same number on both platforms. | Must be unique and increasing for both stores. The offset leaves room for the first manual Play upload (see 1.6). Re-running a failed job reuses the same run and the same artifact, so it never needs a new number. |

### 1.3 Accounts and credentials Ricardo needs to provide

**Apple**
1. Apple Developer Program membership (€99/year), organisation or individual (D3).
2. An **App ID** `nl.scoutingwilo.ssskl` (Certificates, Identifiers & Profiles). No extra capabilities are needed today (no push entitlement, the app doesn't use APNs yet).
3. An **Apple Distribution certificate**, exported as `.p12` with a password.
4. An **App Store Connect API key** (Users and Access → Integrations, role *App Manager*): the `.p8` file, its Key ID and the Issuer ID.
5. An **app record** in App Store Connect (name "SSSKL", bundle ID above, SKU, primary language Dutch).
6. A **TestFlight internal testing group** with the testers (internal testers need an App Store Connect user; external testers need a one-time beta review).
7. Store listing: privacy policy URL, support URL, description, keywords, iPhone 6.9" screenshots, App Privacy answers, age rating, and a **demo account for App Review** (a seeded User-role account; Turnstile must let the reviewer log in).

**Google**
1. Google Play Console developer account ($25 once), organisation or personal (D3).
2. The app created in Play Console with package `nl.scoutingwilo.ssskl`, **Play App Signing** enabled (Google keeps the app signing key).
3. An **upload keystore** generated once by Ricardo (`keytool -genkeypair -v -keystore ssskl-upload.keystore -alias ssskl-upload -keyalg RSA -keysize 4096 -validity 10000`), kept in a password manager too: its file, store password, alias and key password.
4. A **Google Cloud service account** with a JSON key, invited in Play Console (Users and permissions) with *Release apps to testing tracks* and *Release to production* for this app.
5. An **internal testing** track with a tester list (Google group or e-mail list).
6. Store listing: privacy policy URL, short and full description, feature graphic, phone screenshots, Data safety form, content rating questionnaire, target audience, and app access instructions with the same demo account.

**Sentry**: the existing `SENTRY_AUTH_TOKEN` repository secret, now also passed to the release builds.

### 1.4 GitHub environments and secrets

Each stage is a job bound to its own environment. Secrets live in the environment that uses them, so they are only released after that environment's protection rules pass.

| Environment | Protection | Secrets / variables |
|---|---|---|
| `mobile-signing` | Deployment tags: `v*` only. No reviewers, so builds start straight away. | `IOS_DIST_CERT_P12_BASE64`, `IOS_DIST_CERT_PASSWORD`, `ASC_KEY_ID`, `ASC_ISSUER_ID`, `ASC_KEY_P8` (to download the provisioning profile), `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD` |
| `ios-testflight` | Required reviewer: Ricardo. Tags `v*`. | `ASC_KEY_ID`, `ASC_ISSUER_ID`, `ASC_KEY_P8` |
| `ios-app-store` | Required reviewer: Ricardo. Tags `v*`. | same ASC key |
| `android-internal` | Required reviewer: Ricardo. Tags `v*`. | `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` |
| `android-production` | Required reviewer: Ricardo. Tags `v*`. | same service account |

Notes:
- Ricardo is the only reviewer, so leave *Prevent self-review* off.
- A pending approval expires after 30 days; the run then fails, which is harmless.
- `SENTRY_AUTH_TOKEN` stays a repository secret.
- The workflow never runs on `pull_request`, so forks never see these secrets.

### 1.5 The release workflow (`.github/workflows/mobile-release.yml`)

```
tag vX.Y.Z on master
  └─ version (reusable version.yml)
       ├─ build-android  [mobile-signing]  signed .aab ──► android-internal [approve] ──► android-production [approve, stable only]
       └─ build-ios      [mobile-signing]  signed .ipa ──► ios-testflight   [approve] ──► ios-app-store      [approve, stable only]
```

The four store jobs only move an artifact that was built and signed once; they never rebuild. What is approved for production is byte-for-byte what testers had.

**Triggers**: `push: tags: ['v*']` and `workflow_dispatch` (run from a tag, for retries). `version.yml` already fails for a tag that isn't on master.

**`build-android`** (`ubuntu-latest`, environment `mobile-signing`)
1. Checkout, setup .NET 11 and Java 17, `dotnet workload install maui-android`.
2. `dotnet test Mobile/SSSKL.Mobile.Test` (a release never ships with failing Core tests).
3. Decode the keystore to `$RUNNER_TEMP/upload.keystore`.
4. Publish:
   ```bash
   dotnet publish Mobile/SSSKL.Mobile -f net11.0-android -c Release \
     -p:SssklTargetFrameworks=net11.0-android \
     -p:Version=$VERSION -p:ApplicationVersion=$BUILD_NUMBER \
     -p:AndroidPackageFormat=aab -p:AndroidKeyStore=true \
     -p:AndroidSigningKeyStore=$RUNNER_TEMP/upload.keystore \
     -p:AndroidSigningKeyAlias=$ANDROID_KEY_ALIAS \
     -p:AndroidSigningStorePass=env:ANDROID_KEYSTORE_PASSWORD \
     -p:AndroidSigningKeyPass=env:ANDROID_KEY_PASSWORD
   ```
   (`env:` keeps the passwords out of the command line and the binlog.) `SENTRY_AUTH_TOKEN` is set, so symbols upload to `ssskl-mobile`.
5. Upload the `.aab` and the mapping/symbols as a workflow artifact `android-aab`.

**`build-ios`** (`macos-latest` or a pinned image with Xcode 27.2+, environment `mobile-signing`)
1. Checkout, setup .NET 11, select Xcode (as in `mobile.yml`), `dotnet workload install maui-ios`.
2. Import the distribution certificate into a temporary keychain (`apple-actions/import-codesign-certs`).
3. Download the App Store provisioning profile for `nl.scoutingwilo.ssskl` with the ASC API key (`apple-actions/download-provisioning-profiles`), so no profile has to be renewed by hand in secrets.
4. Publish:
   ```bash
   dotnet publish Mobile/SSSKL.Mobile -f net11.0-ios -c Release -r ios-arm64 \
     -p:SssklTargetFrameworks=net11.0-ios \
     -p:Version=$VERSION -p:ApplicationVersion=$BUILD_NUMBER \
     -p:ArchiveOnBuild=true \
     -p:CodesignKey="Apple Distribution" \
     -p:CodesignProvision="<profile name>"
   ```
5. Upload the `.ipa` (and dSYMs) as artifact `ios-ipa`. Delete the temporary keychain in an `if: always()` step.

**`android-internal`** (environment `android-internal`, needs `build-android`)
- `fastlane supply --aab app.aab --track internal --release_status completed --skip_upload_metadata --skip_upload_images --skip_upload_screenshots`.
- The release notes come from `Mobile/fastlane/metadata/android/nl-NL/changelogs/default.txt` (or are generated from the tag message).

**`android-production`** (environment `android-production`, needs `android-internal`, `if: needs.version.outputs.stable == 'true'`)
- `fastlane supply --track internal --track_promote_to production --version_code $BUILD_NUMBER --rollout 0.2` for a staged 20% rollout, or `--release_status completed` for everyone at once. Promoting instead of re-uploading is what keeps the exact tested build.
- Google still reviews the release, which usually takes hours to a few days.

**`ios-testflight`** (environment `ios-testflight`, needs `build-ios`)
- `fastlane pilot upload --ipa SSSKL.ipa --api_key_path asc.json --distribute_external false --changelog "<tag message>"`.
- `pilot` waits for App Store Connect to finish processing; then the build is available to the internal group.

**`ios-app-store`** (environment `ios-app-store`, needs `ios-testflight`, stable only)
- `fastlane deliver --build_number $BUILD_NUMBER --app_version $VERSION --submit_for_review --automatic_release false --skip_screenshots --skip_metadata --force --submission_information '{"add_id_info_uses_idfa": false}'`.
- This creates the App Store version, attaches the tested build and submits it to App Review. *Manual release* is kept, so after approval Ricardo presses "Release" in App Store Connect (or switch `--automatic_release true`).

Shared:
- `concurrency: mobile-release-${{ github.ref }}` with `cancel-in-progress: false`.
- `permissions: contents: read`.
- Every job uses `timeout-minutes`; the upload jobs ~30, the iOS build ~60.
- Job summaries show version, build number and a link to TestFlight / Play Console.

### 1.6 Repository changes

1. **Versioning in `SSSKL.Mobile.csproj`**
   - `ApplicationDisplayVersion` → the numeric part of `$(Version)` (`$(_SssklNumericVersion)` from `Directory.Build.props`). iOS rejects a `CFBundleShortVersionString` like `1.5.0-rc.1`; today a pre-release tag would fail the upload.
   - `ApplicationVersion` default `1`, overridden by `-p:ApplicationVersion=$BUILD_NUMBER` in CI.
2. **`Platforms/iOS/PrivacyInfo.xcprivacy`** (new). Apple rejects uploads without a privacy manifest when the app uses "required reason" APIs, and the .NET runtime does (file timestamps, system boot time, disk space). Declare those reasons plus the collected data types (name, e-mail, user ID, crash data for Sentry, photos for the profile picture), and include it as a `BundleResource`.
3. **`Info.plist`**: `UIDeviceFamily` = `1` only (D5). Keep `ITSAppUsesNonExemptEncryption` false (already set). Check that `NSAllowsLocalNetworking` is fine for review (it is; it only covers local addresses).
4. **Android**: confirm the target SDK that MAUI 11 sets meets Google Play's current requirement (API 36 for new apps and updates from 31 Aug 2026) and that the AOT build supports 16 KB memory pages (required for apps targeting Android 15+). Add `SssklAndroidAot` (D6).
5. **`Mobile/fastlane/`**: `Gemfile` + `Gemfile.lock` (fastlane pinned), `Appfile`, a small `Fastfile` with lanes `ios_testflight`, `ios_submit`, `android_internal`, `android_promote`, and `metadata/` for release notes. Store listing text and screenshots can live here later (`deliver` / `supply` can upload them), but start with the listing filled in by hand.
6. **`mobile.yml`**: restore the `push`/`pull_request` triggers once the app is ready, so every PR proves the app still builds. Keep it unsigned.
7. **Docs**: a `docs/mobile-release.md` runbook (how to cut a release, approve stages, rotate keys) and a short section in `CLAUDE.md`.

### 1.7 One-time manual steps before the first automated run

- **Google Play does not accept the first upload through the API.** Build one signed `.aab` (run the workflow up to `build-android` and download the artifact, or build locally with `-p:ApplicationVersion=1`) and upload it to the internal track in Play Console. After that, CI takes over.
- Fill in the store listing, Data safety, content rating and app access sections on both stores; production submission fails while any is incomplete.
- Apple: request Unlisted App Distribution (D4) after the app record exists.
- Personal Google account only (D3): run the 12-tester / 14-day closed test, then apply for production access.

### 1.8 App Review risks to prepare for

- **4.2 Minimum functionality**: Apple rejects apps that are mostly a website in a WebView. The native POS, notifications, profile, events, quotes, stats and leaderboard should carry it; mention them in the review notes. Unlisted distribution (D4) helps.
- **5.1.1(v) Account deletion**: the app must offer account deletion. The SPA's settings has the personal-data tab with delete; make sure the native settings page links to it clearly.
- **Login for reviewers**: provide a demo account; check that Turnstile and 2FA don't block it.
- **Guideline 3.1 (in-app purchase)** doesn't apply: saldo pays for physical goods and is topped up outside the app. Say so in the review notes to avoid questions.

### 1.9 Implementation order

1. **PR A: release prerequisites.** csproj versioning, privacy manifest, iPhone-only, AOT switch, fastlane folder, runbook. Verifiable with the existing `mobile.yml`.
2. **Ricardo**: accounts, keys and secrets from 1.3 and 1.4; app records; the first manual Play upload.
3. **PR B: `mobile-release.yml`** with the build jobs only. Test by pushing a pre-release tag (`v0.x.y-rc.1`) and downloading the artifacts.
4. **PR C: the four store jobs.** Test with a pre-release tag (only TestFlight / internal run), then the first stable tag.
5. Once the app is out of development: restore the `mobile.yml` triggers and update `CLAUDE.md` (the "kept out of CI and deploys" note).

---

## Part 2: Visual polish inventory

PR #123 already did: hiding the SPA header and menu in the app, native "Meer" and settings pages, one reused WebView with fade-in and native back, Aura slate/zinc colours, borderless entries, Tabler icons, the saldo gradient (`ab8941b`) and the new icon and splash (`ac256c5`, `7a13df3`). None of that is repeated below.

Priority: **H** = noticeable on every use, **M** = clearly better, **L** = nice to have.

### Feedback and motion

| # | Item | Where | Pri |
|---|------|-------|-----|
| P1 | **Haptics.** No haptic feedback anywhere. Add a light tick on selecting a product/person, a success notification on "Bestelling geplaatst", and an error buzz on failures (`HapticFeedback.Default.Perform`, behind a Core `IHaptics` port so view models stay testable). | `PosViewModel`, `Pressable`, `UserDialogs` | H |
| P2 | **Order confirmation.** A placed order is only a short toast. Show a brief success state in the pay panel (check icon, amount, new saldo) and animate the saldo change on the user's row. This is the most-used action in the app. | `PosPage`, `SaldoCard` | H |
| P3 | **Skeleton loading** instead of a centred `ActivityIndicator` on first load: grey placeholder tiles/rows in the page's layout, faded out when data arrives. One reusable `SkeletonView`. | All list pages, `PosPage`, `ProfilePage`, `StatsPage` | H |
| P4 | **Inline load errors with retry** instead of a modal "Laden mislukt" alert (`PosViewModel` line ~125 and the other view models). A full-page error state (icon, text, "Opnieuw") for first loads; a toast for refresh failures when data is already shown. | `PagedListViewModel`, view models, new `ErrorStateView` | H |
| P5 | **List item appearance**: fade/slide-in for newly loaded pages of items, and animated removal when an order is deleted. | `CollectionView`s | L |

### Consistency

| # | Item | Where | Pri |
|---|------|-------|-----|
| P6 | **One empty-state control.** Notifications has icon + title + hint; Orders and others have a bare muted label. Make an `EmptyStateView` (icon, title, hint, optional action) and use it on every list. | Orders, TopUps, Leaderboard, Quotes, SaldoList, Achievements, Events | M |
| P7 | **Segmented control as a control.** The period switch on the leaderboard and the colour-mode switch in settings are hand-written `Border` + trigger blocks. Extract `SegmentedControl` with an animated selection pill. | `LeaderboardPage`, `SettingsPage` | M |
| P8 | **Dark-mode colour slips**: negative saldo uses `{StaticResource Danger}` without the dark variant on Profiel and Meer; the POS check mark is `Primary700` in dark mode too and is a text "✓" rather than a Tabler icon. | `ProfilePage.xaml:51`, `MorePage.xaml:50`, `PosPage.xaml` | H |
| P9 | **Type scale.** Sizes are set inline (13/15/16/18/20/22/30) across pages. Define a small scale (Caption, Body, BodyStrong, Title, LargeTitle, Display) in `Styles.xaml` and replace inline `FontSize`s, so every page reads the same. | `Styles.xaml`, all pages | M |
| P10 | **Selected tab icons.** Tabs use one outline icon for both states. Use Tabler's filled variants for the selected tab (iOS supports a selected image; on Android a selector drawable). | `AppShell`, `Resources/Images/tab_*` | L |

### Screens

| # | Item | Where | Pri |
|---|------|-------|-----|
| P11 | **POS product tiles.** Text-only tiles with a 1 px border. Give them a product icon or image (the API has product data; fall back to an initial), a larger price, and a quantity badge when selected. Consider a sticky product/people segmented switch on small phones so the pay panel isn't pushed below a long list. | `PosPage` | H |
| P12 | **Welcome page.** A single card on a flat background. Use the full screen: the logo on the brand colour `#212529` continuing from the splash, so launch → welcome has no colour jump, with the button pinned to the bottom safe area. | `WelcomePage` | M |
| P13 | **Profile header.** Three equal stat cards get cramped on small phones and with large text. Use one card with three columns and dividers, or a horizontally scrolling row; move "Foto wijzigen/verwijderen" into a tap on the avatar with an action sheet. | `ProfilePage` | M |
| P14 | **iOS large titles** on the four tab roots (`ios:Page.LargeTitleDisplay="Always"`), collapsing on scroll, so the native screens feel like iOS apps. | Tab root pages | L |
| P15 | **Tablet layout**: a maximum content width (e.g. 640) centred on wide screens, needed before iPad support returns (D5). | Page styles | L |
| P16 | **Image loading**: avatars and event images pop in. Fade them in once loaded and show the initials/placeholder until then; cache event images. | `AvatarView`, `EventsPage`, `EventDetailPage` | M |

### Platform finish

| # | Item | Where | Pri |
|---|------|-------|-----|
| P17 | **Android edge-to-edge and system bars.** Apps targeting API 35+ are drawn edge-to-edge. Check the status and navigation bars on Android 15/16 (icon contrast in light and dark mode, content under the gesture bar), and make the bar colours and icons follow the in-app colour mode. | `Platforms/Android`, `MainActivity` | H |
| P18 | **Splash → first screen**: the splash is `#212529`, the first page is slate-50 or zinc-950. Match the splash background to the page colour per theme, or fade from the brand colour (pairs with P12). | `SSSKL.Mobile.csproj`, `App.xaml.cs` | M |
| P19 | **Dynamic Type / font scaling**: check every page at the largest accessibility size; cap scaling on fixed-size elements (tab badges, stepper, avatar initials) with `FontAutoScalingEnabled="False"` and let cards wrap. | All pages | M |
| P20 | **Accessibility pass**: `SemanticProperties` are present but uneven (one on Notifications, eight on POS). Give every tappable `Pressable` a description and every page a level-1 heading; test with VoiceOver and TalkBack. | All pages | M |
| P21 | **Store assets** (needed for 1.3 anyway): consistent screenshots in light mode with real-looking seed data (the E2E seeder can feed a screenshot build), plus Android's feature graphic. | `Mobile/fastlane/metadata` | M |

### Suggested polish batches

1. **Batch 1 (H items):** P1, P2, P3, P4, P8, P17. Done in PR #124; still needs a check on a device.
2. **Batch 2:** P6, P7, P9, P11, P12, P18. Done in PR #124 (P11 without product images, which the API doesn't have, and without the sticky switch; P18 only for the signed-out path: the splash colour is one colour, so a signed-in start still goes from the brand colour to the page colour). Still needs a check on a device.
3. **Batch 3:** P13, P16, P19, P20, P21, then P5, P10, P14, P15.

None of this can be checked in the cloud environment (no MAUI workloads or emulators), so each batch needs a check on a device or simulator before merging; the TestFlight / internal track from Part 1 is the natural place for that.
