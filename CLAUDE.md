# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

SSSKL is a POS / club management system for Scouting Wilo: a .NET 10 ASP.NET Core API (onion architecture across `SSSKL.Domain`, `SSSKL.Contracts`, `SSSKL.Application`, `SSSKL.Infrastructure` and `SSSKL.Web`) plus an Angular 22 SPA (`Frontend/`), orchestrated locally with .NET Aspire (`SSSKL.AppHost/`). The solution file is `SSSKL.slnx`. Package versions are centrally managed in `Directory.Packages.props` (add `<PackageReference>` without a version in csproj files); shared MSBuild properties (version, SixLabors license path) live in `Directory.Build.props`.

## Commands

### Backend (run from repo root)
```bash
dotnet run --project SSSKL.AppHost          # full stack: SQL Server, Azurite, API (:5251), frontend (:4200)
dotnet build SSSKL.slnx
dotnet test SSSKL.slnx
dotnet test SSSKL.Application.Test --filter "FullyQualifiedName~OrderServiceTests"          # one class
dotnet test SSSKL.Application.Test --filter "FullyQualifiedName~OrderServiceTests.CreateOrder" # one method/prefix
dotnet ef migrations add <Name> --project SSSKL.Infrastructure --startup-project SSSKL.Web --output-dir Persistence/Migrations
```
- The build needs a SixLabors ImageSharp license at the **repo root** as `sixlabors.lic` (see `SIXLABORS_LICENSE_SETUP.md`).
- Scalar API docs: `http://localhost:5251/scalar` (Development only).
- MediatR (v13+) is commercially licensed; the Community license key is read from `MediatR:LicenseKey` (user-secrets locally, App Service / App Configuration in production). Without it MediatR only logs a warning, so tests and CI don't need it.
- Migrations are applied automatically on startup (`db.Database.MigrateAsync()` in `SSSKL.Web/Program.cs`) and live in `SSSKL.Infrastructure/Persistence/Migrations/`. The `dotnet-ef` tool is pinned in `.config/dotnet-tools.json` (`dotnet tool restore`).

### Frontend (run from `Frontend/`)
```bash
npm start                  # runs setup-ssl.js then ng serve on $PORT (Aspire sets PORT=4200)
npm run build              # output: dist/SSSKL/browser
npm test                   # Vitest via @angular/build:unit-test, single run
npm run test:coverage
npm run test:update        # update snapshots
npx ng test --no-watch --include src/app/features/orders/services/order.service.spec.ts   # single spec
```
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
- **`SSSKL.Web`**: composition root. `Program.cs`, controllers (`Controllers/v1/`, `[Route("v1/[controller]")]`, mounted under a `/api` group so real routes are `/api/v1/...`) that read claims, parse input and send requests through `ISender`, Identity endpoints incl. passkeys at `/api/v1/identity` (`Registrations/IdentityApiEndpointRouteBuilderExtensions.cs`), and the SignalR hub `Hubs/LiveMetricsHub` at `/hubs/livemetrics`, fed by singleton `Services/PurchaseNotifier`/`EventNotifier`. Production uses Azure SignalR.
- **Background jobs**: `Features/Users/Stats/StatsRecalculationJob` and `Features/Orders/Export/OrderCsvExportJob` are singletons that track jobs in memory and run work on background threads using `IServiceScopeFactory` (one active job at a time; frontend polls status). Controllers reach them through start/status commands and queries.
- **Auth**: ASP.NET Core Identity with a `BearerOrCookie` policy scheme (Bearer header → bearer token, otherwise Identity cookie). Roles are seeded on startup. Antiforgery: a middleware issues a readable `XSRF-TOKEN` cookie; clients send `X-XSRF-TOKEN`.
- **Environments**: `IntegrationTest` is treated like Development for cookie security and skips SQL Server registration/migrations (uses `EnsureCreated`). Blob storage is configured from Aspire's `blobs` connection string, `Storage:ConnectionString`, or `Storage:ServiceUri` (managed identity), in that order.
- **Telemetry**: `SssklTelemetry.Name` (`"SSSKL"`) must match `BusinessTelemetryName` in `SSSKL.ServiceDefaults/Extensions.cs`, which subscribes to it.

## Backend tests

MSTest + FluentAssertions + NSubstitute + Bogus, one test project per layer:
- **`SSSKL.Application.Test`**: handler tests (`Features/<Feature>/`), shared-service, event handler, validator and util tests; dependencies mocked with NSubstitute, no database. Handlers are internal (`InternalsVisibleTo`); use `NullLogger<T>.Instance` for their loggers, because NSubstitute can't proxy `ILogger<T>` of an internal type.
- **`SSSKL.Infrastructure.Test`**: repository tests inherit `Util/RepositoryTest` (in-memory SQLite, `InitializeDatabase()`/`CleanupDatabase()`, seeded `TestUser`/`TestProduct`) and use `Util/MockDbContextFactory`; also agent, web push and Turnstile tests, plus handler tests (`Features/`: reactions, notifications, roles, settings, blobs) that run against real repositories.
- **`SSSKL.Web.Test`**: controller tests that mock `ISender`, and integration tests (`Integration/`, incl. `MediatRPipelineTests`) that use `WebApplicationFactory<Program>` with `UseEnvironment("IntegrationTest")`, replacing the DbContext/factory registrations with a shared in-memory SQLite connection.
- CI (`.github/workflows/dotnet.yml`) excludes Migrations, Controllers, `Program.cs` and `DependencyInjection.cs` from Sonar coverage.

## Frontend architecture (`Frontend/src/app/`)

- `core/`: auth (`auth.service`, `auth.interceptor`, guards incl. `role.guard`), `api-base.interceptor` (prefixes `/api/` and `/hubs/` requests with `environment.apiUrl`, which is injected at build time via `CUSTOM_API_URL`), shared models, and app-wide services.
- `features/<feature>/`: lazy-loaded feature areas, each with its own components and a `services/` folder for its HTTP service.
- `layout/` (shell, header, sidebar) and `shared/` (components, directives, pipes, utils).
- UI uses PrimeNG + Tailwind (`tailwindcss-primeui`). UI strings are Dutch, provided by `core/services/language.service.ts` (`translate(key)`); add new keys there.
- PWA with service worker (`ngsw-config.json`) and web push notifications.

Follow `Frontend/AGENTS.md` for Angular code. Key rules: standalone components (don't set `standalone: true`), `ChangeDetectionStrategy.OnPush`, signals + `computed()` for state, `input()`/`output()` functions, `inject()` instead of constructor injection, native control flow (`@if`/`@for`), `class`/`style` bindings instead of `ngClass`/`ngStyle`, `host` object instead of `@HostBinding`/`@HostListener`, reactive forms, and WCAG AA / AXE compliance.

## Versioning & deployment

- The app version is kept in `Directory.Build.props` (`Version`, `InformationalVersion`, `AssemblyVersion`, `FileVersion`; update all four). `Frontend/package.json` has its own version field.
- Commits follow Conventional Commits (`feat:`, `fix:`, `chore:`).
- Frontend deploys to Azure Static Web Apps, backend to Azure App Service, both from `master` via GitHub Actions.
