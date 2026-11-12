# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

SSSKL is a POS / club management system for Scouting Wilo: a .NET 11 ASP.NET Core API in `Backend/` (onion architecture across `SSSKL.Domain`, `SSSKL.Contracts`, `SSSKL.Application`, `SSSKL.Infrastructure` and `SSSKL.Web`) plus an Angular 22 SPA (`Frontend/`) and a .NET MAUI mobile app (`Mobile/`, see below), orchestrated locally with .NET Aspire (`Backend/SSSKL.AppHost/`). Backend project paths below are relative to `Backend/` unless they start with `Backend/`. Docs live in `docs/` (Docker and releases in `docs/docker.md`). The backend solution `SSSKL.slnx` sits at the repo root (Mobile isn't in it), next to `Directory.Build.props`, `Directory.Packages.props` and `.config/`, which Backend and Mobile share. Package versions are centrally managed in `Directory.Packages.props` (add `<PackageReference>` without a version in csproj files); shared MSBuild properties (version, SixLabors license path) live in `Directory.Build.props`.

## Commands

### Backend (run from repo root)
```bash
dotnet run --project Backend/SSSKL.AppHost  # full stack: SQL Server, Azurite, API (:5251), frontend (:4200)
dotnet build SSSKL.slnx
dotnet test SSSKL.slnx
dotnet test Backend/SSSKL.Application.Test --filter "FullyQualifiedName~OrderServiceTests"          # one class
dotnet test Backend/SSSKL.Application.Test --filter "FullyQualifiedName~OrderServiceTests.CreateOrder" # one method/prefix
dotnet ef migrations add <Name> --project Backend/SSSKL.Infrastructure --startup-project Backend/SSSKL.Web --output-dir Persistence/Migrations
```
- The build needs a SixLabors ImageSharp license at the **repo root** as `sixlabors.lic` (see `docs/SIXLABORS_LICENSE_SETUP.md`).
- Scalar API docs: `http://localhost:5251/scalar` (Development only).
- MediatR (v13+) is commercially licensed; the Community license key is read from `MediatR:LicenseKey` (user-secrets locally, App Service setting `MediatR__LicenseKey` in production; see `docs/MEDIATR_LICENSE_SETUP.md`). Without it MediatR only logs a warning, so tests and CI don't need it.
- Migrations are applied automatically on startup (`db.Database.MigrateAsync()` in `SSSKL.Web/Program.cs`) and live in `SSSKL.Infrastructure/Persistence/Migrations/`. The `dotnet-ef` tool is pinned in `.config/dotnet-tools.json` (`dotnet tool restore`).

### Frontend (run from `Frontend/`)
```bash
npm start                  # runs setup-ssl.js then ng serve on $PORT (Aspire sets PORT=4200)
npm run build              # output: dist/SSSKL/browser
npm test                   # Vitest via @angular/build:unit-test, single run
npm run test:coverage
npm run test:update        # update snapshots
npx ng test --no-watch --include src/app/features/orders/services/order.service.spec.ts   # single spec
npm run e2e:install        # once: Playwright Chromium
npm run e2e                # Playwright; starts the API (E2E env, :5252) and ng serve (:4300), or reuses running ones
npx playwright test e2e/pos-order.e2e.ts   # single e2e spec
```
- E2E specs live in `Frontend/e2e/*.e2e.ts` (outside `src/`, so Vitest and Sonar skip them). `auth.setup.ts` stores a logged-in state per role in `e2e/.auth/`; seeded users are mirrored in `e2e/support/users.ts`. Specs run serially against one database and must tolerate data left by earlier runs when servers are reused (assert deltas, use unique names).
- `npm start` and `npm run build` go through `ng-env.js`, which injects `PRIMEUI_LICENSE_KEY` (from the environment or gitignored `Frontend/.env`, see `.env.example`) as a build-time define. The Static Web Apps deploy reads it from the `PRIMEUI_LICENSE_KEY` repo secret; without it the app builds but PrimeUI shows a license banner.

## Backend architecture

Onion architecture; project references only point inwards, so the compiler enforces the layering:

```
SSSKL.Domain ← SSSKL.Contracts ← SSSKL.Application ← SSSKL.Web
                     ↑                                    │
             SSSKL.Infrastructure ←───────────────────────┘
```

- **`SSSKL.Domain`**: entities (`Entities/`), domain events (`Events/`: `OrderPlacedEvent`, `TopUpEvent`, `QuoteCreatedEvent`, `QuoteVotedEvent`, `ReactionAddedEvent`), constants (`Constants/Roles.cs`: Admin, Kiosk, User, Guest; `GlobalSettingsKeys`) and `Exceptions/` (`NotFoundException`, `ConflictException`). Domain events implement `IDomainEvent : MediatR.INotification`. Only depends on `Microsoft.Extensions.Identity.Stores` (for `IdentityUser`/`IdentityRole`) and `MediatR.Contracts`; no EF Core or ASP.NET Core. Put EF mapping in `ApplicationDbContext.OnModelCreating`, not as EF attributes on entities.
- **`SSSKL.Contracts`**: DTOs (`Dto/`, `Dto/Api/v1/`, paged lists return `PaginationObject<T>` with `skip`/`take`), and the interfaces of repositories (`Repositories/`), ports implemented in Infrastructure/Web (`Services/`: `IPurchaseNotifier`, `IEventNotifier`, `IWebPushService`, `ITurnstileService`), agents (`Agents/IBlobStorageAgent`) and `Events/IDomainEventDispatcher`. `Telemetry/SssklTelemetry` holds business metrics.
- **`SSSKL.Application`**: CQRS with MediatR. Each use case is a command or query record plus an `internal sealed` handler in `Features/<Feature>/Commands|Queries/<UseCase>/` (one file each), with entity-to-DTO mappings in `Features/<Feature>/<Feature>Mappings.cs`. Queries return DTOs and don't change state. Handlers use repository interfaces and never call `ISender`; logic shared between handlers lives in `Shared/` (`IUserNotifier`, `IAchievementAwarder`, `IUserRoleProvider`) or a feature-level helper (e.g. `QuoteAccess`, `EventAccess`). Handlers signal failures with `NotFoundException` (404), `UnauthorizedAccessException` (403), `ArgumentException` (400) and `ConflictException` (409), which `GlobalExceptionHandler` maps. `EventHandlers/AchievementEventHandler` handles every domain event (`INotificationHandler<T>`) to update `UserStat`/`ProductUserStat` and award achievements; `Events/DomainEventDispatcher` publishes in a new scope and `Events/ResilientNotificationPublisher` logs and swallows handler failures. `Behaviors/LoggingBehavior` logs every request. Also FluentValidation validators (`Validators/`) and `Util/`. Must not reference EF Core or ASP.NET Core. Handlers and notification handlers are registered by assembly scan in `DependencyInjection.cs` (`AddApplication`); register new shared services there.
- **`SSSKL.Infrastructure`**: `Persistence/ApplicationDbContext`, migrations, repositories (`Persistence/Repositories/`), `Agents/BlobStorageAgent`, and external services (`SmtpEmailSender`, `WebPushService`, `TurnstileService`). `DependencyInjection.cs` (`AddInfrastructure` on `IHostApplicationBuilder`) registers SQL Server, blob storage, repositories and these services. Most repositories use `IDbContextFactory<ApplicationDbContext>` (a context per operation); `QuoteRepository`, `ReactionRepository`, `NotificationRepository`, `PushSubscriptionRepository`, `RoleRepository`, `GlobalSettingRepository`, `UserImageRepository` and `BlobStorageItemRepository` use the scoped context so tracked entities (roles, the Identity user) share a unit of work. Repositories throw `NotFoundException`, which `GlobalExceptionHandler` translates to 404.
- **`SSSKL.Web`**: composition root. `Program.cs`, controllers (`Controllers/v1/`, `[Route("v1/[controller]")]`, mounted under a `/api` group so real routes are `/api/v1/...`) that read claims, parse input and send requests through `ISender`, Identity endpoints incl. passkeys at `/api/v1/identity` (`Registrations/IdentityApiEndpointRouteBuilderExtensions.cs`), and the SignalR hub `Hubs/LiveMetricsHub` at `/hubs/livemetrics`, fed by singleton `Services/PurchaseNotifier`/`EventNotifier`. Production uses Azure SignalR unless `Azure:SignalR:Enabled` is false (the Docker image), then the API serves the hub itself.
- **Background jobs**: `Features/Users/Stats/StatsRecalculationJob` and `Features/Orders/Export/OrderCsvExportJob` are singletons that track jobs in memory and run work on background threads using `IServiceScopeFactory` (one active job at a time; frontend polls status). Controllers reach them through start/status commands and queries.
- **Auth**: ASP.NET Core Identity with a `BearerOrCookie` policy scheme (Bearer header → bearer token, otherwise Identity cookie). Roles are seeded on startup. Antiforgery: a middleware issues a readable `XSRF-TOKEN` cookie; clients send `X-XSRF-TOKEN`.
- **Environments**: the test environments `IntegrationTest` and `E2E` (`SSSKL.Infrastructure/Hosting/TestEnvironments`, `IsTestLike()`) are treated like Development for cookie security, run on SQLite from `ConnectionStrings:db` (`SqliteFunctionsInterceptor` adds `GETDATE`/`GETUTCDATE`) and use `EnsureCreated` instead of migrations. `E2E` also deletes and recreates its database on startup, seeds fixed users/products/an event (`Registrations/E2ESeeder`, password from `E2E:UserPassword`, which `playwright.config.ts` sets as `E2E__UserPassword` from `e2e/support/users.ts`; set it yourself when you start the `e2e` profile by hand), stores blobs on disk (`LocalFileBlobStorageAgent`, `Storage:LocalRoot`) and refuses to start on App Service (`E2EGuard`); config is `appsettings.E2E.json` (never published), launch profile `e2e` on http://localhost:5252. Rate limits for the identity endpoints come from the `RateLimiting` section. `Storage:Provider` (`Infrastructure/Agents/BlobStorageProvider`) picks the blob store: `Azure` (the default; E2E defaults to `LocalFile`) or `LocalFile` (files under `Storage:LocalRoot`, used by the Docker image). Azure Blob Storage is configured from Aspire's `blobs` connection string, `Storage:ConnectionString`, or `Storage:ServiceUri` (managed identity), in that order.
- **Telemetry**: `SssklTelemetry.Name` (`"SSSKL"`) must match `BusinessTelemetryName` in `SSSKL.ServiceDefaults/Extensions.cs`, which subscribes to it. KQL queries for Application Insights are in `docs/APPLICATION_INSIGHTS_QUERIES.md`; update it when you add metrics or log messages.

## Backend tests

MSTest + FluentAssertions + NSubstitute + Bogus, one test project per layer:
- **`SSSKL.Application.Test`**: handler tests (`Features/<Feature>/`), shared-service, event handler, validator and util tests; dependencies mocked with NSubstitute, no database. Handlers are internal (`InternalsVisibleTo`); use `NullLogger<T>.Instance` for their loggers, because NSubstitute can't proxy `ILogger<T>` of an internal type.
- **`SSSKL.Infrastructure.Test`**: repository tests inherit `Util/RepositoryTest` (in-memory SQLite, `InitializeDatabase()`/`CleanupDatabase()`, seeded `TestUser`/`TestProduct`) and use `Util/MockDbContextFactory`; also agent, web push and Turnstile tests, plus handler tests (`Features/`: reactions, notifications, roles, settings, blobs) that run against real repositories.
- **`SSSKL.Web.Test`**: controller tests that mock `ISender`, and integration tests (`Integration/`) that send real HTTP requests through `Integration/Infrastructure/SssklWebApplicationFactory` (IntegrationTest environment, one temp SQLite file per factory, fakes for email/web push/blob storage exposed as `Emails`/`Push`/`Blobs`; `WithSetting`/`WithServices` for overrides). Test classes derive from `IntegrationTestBase`, own one static factory (`[ClassInitialize]`/`[ClassCleanup]`) and use its helpers (`CreateAuthenticatedClientAsync(role)`, `SeedProductAsync`, `WithDbAsync`, `PollAsync`). Classes run in parallel (`MSTestSettings.cs`), so give each test its own users and data.
- CI (`.github/workflows/dotnet.yml`) excludes Migrations, Controllers, `Program.cs` and `DependencyInjection.cs` from Sonar coverage.

## Frontend architecture (`Frontend/src/app/`)

- `core/`: auth (`auth.service`, `auth.interceptor`, guards incl. `role.guard`), `api-base.interceptor` (prefixes `/api/` and `/hubs/` requests with `environment.apiUrl`, which is injected at build time via `CUSTOM_API_URL`), shared models, and app-wide services.
- `features/<feature>/`: lazy-loaded feature areas, each with its own components and a `services/` folder for its HTTP service.
- `layout/` (shell, header, sidebar) and `shared/` (components, directives, pipes, utils).
- UI uses PrimeNG + Tailwind (`tailwindcss-primeui`). UI strings are Dutch, provided by `core/services/language.service.ts` (`translate(key)`); add new keys there.
- PWA with service worker (`ngsw-config.json`) and web push notifications.

Follow `Frontend/AGENTS.md` for Angular code. Key rules: standalone components (don't set `standalone: true`), `ChangeDetectionStrategy.OnPush`, signals + `computed()` for state, `input()`/`output()` functions, `inject()` instead of constructor injection, native control flow (`@if`/`@for`), `class`/`style` bindings instead of `ngClass`/`ngStyle`, `host` object instead of `@HostBinding`/`@HostListener`, reactive forms, and WCAG AA / AXE compliance.

## Mobile app (`Mobile/`)

.NET MAUI app for iOS and Android. It talks to the same API.

It is still in development, so it is kept out of CI and deploys: it is not in `SSSKL.slnx` (backend CI, Sonar and the App Service deploy don't build or test it) and has its own solution `Mobile/SSSKL.Mobile.slnx`. Build the MAUI app explicitly with `-f net11.0-android|net11.0-ios`.

**Projects**
- **`SSSKL.Mobile.Core`** (net11.0, no MAUI): the API client (`Api/SssklApiClient`, client-side DTO copies in `Models/`), auth, popup coordinators, view models (CommunityToolkit.Mvvm), Dutch strings (`Localization/Texts`) and the WebView rules (`Web/`).
  - **`Auth/AuthSession`** holds the bearer tokens and refreshes them, one refresh at a time. Sign-in and sign-out bump a session generation; a refresh that finishes after the session changed is discarded (no store write, no `TokensChanged`).
  - **`Auth/AuthTokenHandler`** adds the bearer header and retries once after a refresh on a 401.
- **`SSSKL.Mobile`** (net11.0-android/net11.0-ios): XAML pages, Shell and the platform code (`Platforms/*/WebViewPlatform.*.cs`).
- **`SSSKL.Mobile.Test`**: MSTest tests for Core.

**Native vs. web**
- Native screens: the welcome page (signed out), the tabs Kassa (POS), Notificaties (tab badge = unread count, `Services/NotificationInbox`) and Profiel, and pushed pages: settings, Mijn bestellingen, Mijn saldo, Saldolijst, Leaderboard, Statistieken, Achievements (+ detail), other members' profiles (`users/{id}`, the app's own route), Over SSSKL, Evenementen (+ detail: RSVP, image, delete) and Quotes (+ detail: vote, delete), both with comments (`Controls/CommentsView`); plus the achievement and "what's new" popups.
- The "Meer" tab lists the features without a tab; the login sheet, admin pages and the create/edit forms of events (`events/new`, `events/{id}/edit`) and quotes (`quotes?new`, `quotes?edit=<id>`) show the SPA in a WebView.
- `Navigation/NativeRoutes` decides which SPA routes open natively (menu entries, notification `LinkUri`s, `?userId=` for stats/achievements); `IAppNavigator.OpenAsync(path)` goes through it, pushes the page on the current tab (`Views/INativePage.Prepare` gets the route) and falls back to the WebView.
- Paged lists derive from `ViewModels/PagedListViewModel`; dates and money go through `ViewModels/Display` (Dutch, UTC shown in local time).
- Bind commands of toolkit behaviors (`TouchBehavior`) from code, or use `Controls/Pressable`: behaviors don't inherit the page's `BindingContext`.
- Icons are Tabler Icons (MIT) SVGs in `SSSKL.Mobile/Resources/Images` (`ti_*.svg`, tab bar `tab_*.svg`), listed in `Core/Localization/Icons` and shown with `Controls/IconView` (tinted at runtime). Copy new ones from the `@tabler/icons` npm package's `icons/outline/` with `stroke` set to `#000000` and the full MIT notice in a comment at the top; a test checks every icon file for it.

**Login**
- Signed out (also after sign-out, session expiry and the SPA's `ssskl-app://logout`), the window root is the native `Views/WelcomePage` (logo, version, "Inloggen").
- "Inloggen" opens `Views/LoginPage` as a modal sheet (page sheet on iOS, swipe-down closes it; close button and Android back close it too). A new `LoginPage` is resolved per tap.
- The sheet uses the SPA's own `/login` page, so Turnstile, 2FA and passkeys keep working. When it has the tokens it dismisses itself and `AppNavigator.CompleteSignInAsync` switches the root to the `AppShell`.
- After login the app reads `access_token`/`refresh_token` from the WebView's storage and keeps them in SecureStorage.
- The app then injects them into every SPA page with a document-start script (`Web/WebSessionScript`). That script also sets `window.SSSKL_APP`.

**SPA embedded mode** (`core/services/embedded.service.ts`)
- Detected by `window.SSSKL_APP` or the `SSSKLApp/` user-agent token.
- In embedded mode the SPA skips its achievement and what's-new modals, the push prompt and the service worker.
- It hands `/pos` (`core/auth/native-route.guard.ts`) and logout over to the app by navigating to `ssskl-app://<action>`. `Web/WebNavigationPolicy` intercepts these navigations.
- `nativePageGuard` (same file) hands routes the app has natively back with `ssskl-app://open?path=<url>` when the user navigates to them inside the SPA (e.g. to the event a form just saved; `event-edit` goes to the saved event in embedded mode). Pages the app opened itself (the initial URL, or `window.ssskl.navigate`, whose navigations carry the `FROM_APP_STATE` state) render. The app then closes its WebView page and shows the native page (`AppNavigator.OpenFromWebAsync`). Keep the guarded routes in step with `Navigation/NativeRoutes`.
- `quotes?new` / `?edit=<id>` open the quote form dialog; in embedded mode the quotes page hands the result back (`/quotes/<id>`) and resets its URL so the reused WebView can open the form again.
- The SPA only hands logout to the app when the user signs out or the refresh token is rejected (400/401). Other refresh failures (offline, 5xx) don't sign out in embedded mode (`auth.interceptor.ts`).
- `GET achievement/entries/unseen` marks the entries as seen. Only the app's `AchievementPopupCoordinator` may call it in embedded mode.

**Commands**

```bash
dotnet test Mobile/SSSKL.Mobile.Test                                  # no workload needed
sudo dotnet workload install maui-android maui-ios                    # once, for the app itself
cd Frontend && npm run start:mobile                                   # SPA on http://0.0.0.0:4300, proxies /api to $SSSKL_API_URL (default http://localhost:5251)
dotnet build Mobile/SSSKL.Mobile -t:Run -f net11.0-android            # Debug: http://10.0.2.2:4300
dotnet build Mobile/SSSKL.Mobile -t:Run -f net11.0-ios                # Debug: http://localhost:4300
dotnet build Mobile/SSSKL.Mobile -c Release -p:SssklWebUrl=https://... # override URL; API defaults to the SPA origin (-p:SssklApiUrl=...)
```

Release builds default to `https://ssskl.scoutingwilo.nl`.

**Native AOT**
- Release publishes use Native AOT (`PublishAot`); Debug builds don't.
- On iOS it only applies to device publishes (`-r ios-arm64`), not simulator builds. On Android the SDK still marks it experimental (warning XA1040).
- Keep the code reflection-free:
  - compiled bindings with `x:DataType`
  - JSON only through the source-generated `ApiJsonContext` in Core; add new DTO types there
- `SSSKL.Mobile.Core` has `IsAotCompatible` set, so the analyzers flag reflection use.
- The only expected trim warning is IL2104 from CommunityToolkit.Maui. CI is `.github/workflows/mobile.yml` (Core tests and an Android build, plus an unsigned iOS simulator build). For now it only runs by hand (`workflow_dispatch`); the comment in the file shows the push/PR triggers to restore once the app is ready.

**Store releases**
- `.github/workflows/mobile-release.yml` runs on the same `vX.Y.Z` tag as the API. It builds and signs each app once, then moves that build through approval-gated environments: TestFlight → App Store and Play internal testing → production. Pre-release tags stop after testing.
- The uploads use fastlane (`Mobile/Gemfile`, `Mobile/fastlane/Fastfile`).
- The display version is the tag's numbers (`ApplicationDisplayVersion` = `_SssklNumericVersion`). The build number is run number + 1000 (`-p:ApplicationVersion`).
- iOS jobs run on the `xcode-27` runner image with Xcode 27.2; it has no .NET 11, so `setup-dotnet` installs it.
- Setup, secrets and maintenance are in `docs/mobile-release.md`.

## Error monitoring (Sentry)

Sentry org `ricardo-tillemans` (EU region). One project per app: `ssskl-api`, `ssskl-frontend`, `ssskl-mobile`. The DSNs are public client keys and are committed.

**Where each app sets it up**
- **API:** `Sentry.AspNetCore`, configured in `SSSKL.Web/Program.cs` and the `Sentry` section of `appsettings.json`.
  - Sentry is off in the IntegrationTest and E2E environments (`IsTestLike()`).
  - Exceptions that `GlobalExceptionHandler` maps to 4xx are filtered out.
- **SPA:** `@sentry/angular`, set up in `Frontend/src/sentry.ts` and called from `main.ts` before bootstrap. `app.config.ts` provides the error handler and `TraceService`.
  - Sentry is off in automated browsers (`navigator.webdriver`), so Playwright and Vitest runs don't report.
  - Events carry an `embedded` tag, true inside the mobile app.
- **Mobile app:** `Sentry.Maui`, set up with `UseSentry` in `MauiProgram.cs`.

**Releases**
- All three use the app version (the release tag, see below): `SSSKL.Web@<v>`, `SSSKL.Mobile@<v>` and `ssskl-frontend@<v>`. The SPA's is computed in `Frontend/sentry-release.js` (from `APP_VERSION`) and injected by `ng-env.js`.

**Readable stack traces**
- Uploads only run when `SENTRY_AUTH_TOKEN` is set. It's a repository secret holding a Sentry organization auth token.
- **API:** the Release `dotnet publish` in `master_sssklv2-app.yml` uploads PDBs and sources. The settings are in `Backend/SSSKL.Web/SSSKL.Web.csproj`.
- **SPA:** production builds emit hidden source maps. `postbuild` (`Frontend/sentry-sourcemaps.js`) injects Debug IDs, uploads the maps, then deletes all `.map` files so they're never deployed. Injecting changes JS files, so it rehashes `ngsw.json` afterwards (otherwise service-worker updates never install). A Sentry failure only prints a `::warning::`; the build still succeeds.
- **Mobile app:** `SSSKL.Mobile.csproj` uploads symbols in Release builds that have the token set. `mobile.yml` doesn't pass the token, because those builds aren't shipped; `mobile-release.yml` does.

## Versioning & deployment

- The version comes from the git tag; never edit it by hand. `Directory.Build.props` defaults `Version` to `0.0.0` and derives `InformationalVersion`/`AssemblyVersion`/`FileVersion` from it, so `-p:Version=X.Y.Z` sets all four. `Frontend/package.json` stays `0.0.0`; the SPA gets `APP_VERSION` at build time (`ng-env.js` → `CURRENT_APP_VERSION` in `core/services/app-version.service.ts`).
- Release: `git tag vX.Y.Z && git push origin vX.Y.Z` on master. `.github/workflows/version.yml` turns the tag into the version (and fails for a tag that isn't on master). Only a stable `vX.Y.Z` deploys the backend to Azure App Service (`master_sssklv2-app.yml`, deploy job in GitHub environment `production`) and the frontend to Azure Static Web Apps; a plain `master` push deploys nothing to Azure.
- Docker: `Backend/Dockerfile` (API) and `Frontend/Dockerfile` (SPA served by Caddy, `Frontend/Caddyfile` proxies `/api` and `/hubs` to the API) are built from the repo root. The `image` jobs in `dotnet.yml` and `frontend-ci.yml` push them to `ghcr.io/ricardotill/ssskl-api` and `ssskl-frontend` after the tests: `master`/`sha-*` on master, `X.Y.Z`/`X.Y`/`latest` for a stable tag. `docker-compose.yml` runs the full stack (SQL Server; uploads as files in the `api-data` volume), `docker-compose.external.yml` only API + frontend on Azure SQL/Blob Storage; settings in `.env` (`.env.example`). The image sets `DataProtection__KeysPath=/data/keys` (keys survive restarts), `Storage__Provider=LocalFile` with `Storage__LocalRoot=/data/blobs`, and `Azure__SignalR__Enabled=false`; `Hosting:AllowHttp` (`ALLOW_HTTP`) relaxes Secure-only cookies for plain-HTTP tries. See `docs/docker.md`.
- Commits follow Conventional Commits (`feat:`, `fix:`, `chore:`).
