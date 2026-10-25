# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

SSSKLv2 is a POS / club management system for Scouting Wilo: a .NET 10 ASP.NET Core API (`SSSKLv2/`) plus an Angular 22 SPA (`Frontend/`), orchestrated locally with .NET Aspire (`SSSKLv2.AppHost/`). The solution file is `SSSKLv2.slnx`. Package versions are centrally managed in `Directory.Packages.props` (add `<PackageReference>` without a version in csproj files).

## Commands

### Backend (run from repo root)
```bash
dotnet run --project SSSKLv2.AppHost        # full stack: SQL Server, Azurite, API (:5251), frontend (:4200)
dotnet build SSSKLv2.slnx
dotnet test SSSKLv2.slnx
dotnet test SSSKLv2.Test --filter "FullyQualifiedName~OrderServiceTests"          # one class
dotnet test SSSKLv2.Test --filter "FullyQualifiedName~OrderServiceTests.CreateOrder" # one method/prefix
dotnet ef migrations add <Name> --project SSSKLv2                                  # new EF migration
```
- The build needs a SixLabors ImageSharp license at the **repo root** as `sixlabors.lic` (see `SIXLABORS_LICENSE_SETUP.md`).
- Scalar API docs: `http://localhost:5251/scalar` (Development only).
- Migrations are applied automatically on startup (`db.Database.MigrateAsync()` in `Program.cs`). Migrations exist in both `SSSKLv2/Migrations/` and `SSSKLv2/Data/Migrations/`; they share one `ApplicationDbContextModelSnapshot`.

### Frontend (run from `Frontend/`)
```bash
npm start                  # runs setup-ssl.js then ng serve on $PORT (Aspire sets PORT=4200)
npm run build              # output: dist/SSSKLv2/browser
npm test                   # Vitest via @angular/build:unit-test, single run
npm run test:coverage
npm run test:update        # update snapshots
npx ng test --no-watch --include src/app/features/orders/services/order.service.spec.ts   # single spec
```
- `npm start` and `npm run build` go through `ng-env.js`, which injects `PRIMEUI_LICENSE_KEY` (from the environment or gitignored `Frontend/.env`, see `.env.example`) as a build-time define. The Static Web Apps deploy reads it from the `PRIMEUI_LICENSE_KEY` repo secret; without it the app builds but PrimeUI shows a license banner. `TURNSTILE_SITE_KEY` is passed the same way (repo variable `vars.TURNSTILE_SITE_KEY` in the deploy); the API side needs `Turnstile:SecretKey`. With both empty, Turnstile is disabled.

## Backend architecture

Layered: **Controllers → Services → Repositories (DAL) → `ApplicationDbContext`**.

- **Controllers** (`Controllers/v1/`) use `[Route("v1/[controller]")]`, but `Program.cs` mounts all controllers under a `/api` group, so real routes are `/api/v1/...`. Identity endpoints (including passkeys) are mapped at `/api/v1/identity` via `Registrations/IdentityApiEndpointRouteBuilderExtensions.cs`. Controllers map entities to DTOs in `Dto/Api/v1/`; paged lists return `PaginationObject<T>` (`skip`/`take` query params).
- **Services** (`Services/`, interfaces in `Services/Interfaces/`) hold business logic. Register new ones in `Services/ServicesExtensions.cs` (`AddServicesDI`).
- **Repositories** (`Data/DAL/`, interfaces in `Data/DAL/Interfaces/`) use `IDbContextFactory<ApplicationDbContext>` (create a context per operation, not an injected scoped context). Register in `Data/DAL/ServicesExtensions.cs` (`AddDataDI`). Repositories throw `Data/DAL/Exceptions/NotFoundException` which controllers translate to 404.
- **Agents** (`Agents/`) wrap external infrastructure (Azure Blob Storage). Registered via `AddAgentsDI`.
- **Validation**: FluentValidation validators in `Validators/`, auto-registered from the assembly.
- **Domain events** (`Events/`): services publish events (`OrderPlacedEvent`, `TopUpEvent`, `QuoteCreatedEvent`, `QuoteVotedEvent`, `ReactionAddedEvent`) through `IDomainEventDispatcher`, which resolves `IDomainEventHandler<T>` in a new scope. `Services/Handlers/AchievementEventHandler.cs` handles all of them to update `UserStat`/`ProductUserStat` and award achievements. New handlers must be registered explicitly per event type in `AddServicesDI`.
- **Background jobs**: `StatsRecalculationJobService` and `OrderCsvExportJobService` are singletons that track jobs in memory and run work on background threads using `IServiceScopeFactory` (one active job at a time; frontend polls status).
- **Real-time**: SignalR hub `LiveMetricsHub` at `/hubs/livemetrics`, fed by singleton `PurchaseNotifier`/`EventNotifier`. Production uses Azure SignalR.
- **Auth**: ASP.NET Core Identity with a `BearerOrCookie` policy scheme (Bearer header → bearer token, otherwise Identity cookie). Roles are constants in `Data/Constants/Roles.cs` (Admin, Kiosk, User, Guest), seeded on startup. Antiforgery: a middleware issues a readable `XSRF-TOKEN` cookie; clients send `X-XSRF-TOKEN`.
- **Environments**: `IntegrationTest` is treated like Development for cookie security and skips SQL Server registration/migrations (uses `EnsureCreated`). Blob storage is configured from Aspire's `blobs` connection string, `Storage:ConnectionString`, or `Storage:ServiceUri` (managed identity), in that order.

## Backend tests (`SSSKLv2.Test/`)

MSTest + FluentAssertions + NSubstitute + Bogus.
- Repository tests inherit `Util/RepositoryTest` (in-memory SQLite, `InitializeDatabase()`/`CleanupDatabase()`, seeded `TestUser`/`TestProduct`) and use `Util/MockDbContextFactory`.
- Service/controller tests mock dependencies with NSubstitute.
- Integration tests (`Integration/`) use `WebApplicationFactory<Program>` with `UseEnvironment("IntegrationTest")`, replacing the DbContext/factory registrations with a shared in-memory SQLite connection.
- CI (`.github/workflows/dotnet.yml`) excludes Migrations, Controllers, `Program.cs` and `ServiceExtensions.cs` from Sonar coverage.

## Frontend architecture (`Frontend/src/app/`)

- `core/`: auth (`auth.service`, `auth.interceptor`, guards incl. `role.guard`), `api-base.interceptor` (prefixes `/api/` and `/hubs/` requests with `environment.apiUrl`, which is injected at build time via `CUSTOM_API_URL`), shared models, and app-wide services.
- `features/<feature>/`: lazy-loaded feature areas, each with its own components and a `services/` folder for its HTTP service.
- `layout/` (shell, header, sidebar) and `shared/` (components, directives, pipes, utils).
- UI uses PrimeNG + Tailwind (`tailwindcss-primeui`). UI strings are Dutch, provided by `core/services/language.service.ts` (`translate(key)`); add new keys there.
- PWA with service worker (`ngsw-config.json`) and web push notifications.

Follow `Frontend/AGENTS.md` for Angular code. Key rules: standalone components (don't set `standalone: true`), `ChangeDetectionStrategy.OnPush`, signals + `computed()` for state, `input()`/`output()` functions, `inject()` instead of constructor injection, native control flow (`@if`/`@for`), `class`/`style` bindings instead of `ngClass`/`ngStyle`, `host` object instead of `@HostBinding`/`@HostListener`, reactive forms, and WCAG AA / AXE compliance.

## Versioning & deployment

- The app version is kept in `SSSKLv2/SSSKLv2.csproj` (`Version`, `InformationalVersion`, `AssemblyVersion`, `FileVersion`; update all four). `Frontend/package.json` has its own version field.
- Commits follow Conventional Commits (`feat:`, `fix:`, `chore:`).
- Frontend deploys to Azure Static Web Apps, backend to Azure App Service, both from `master` via GitHub Actions.
