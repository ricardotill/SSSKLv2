# Changelog

## 3.11.0 - 2026-10-25

### Added

- Cloudflare Turnstile on login, register, forgot-password and resend-confirmation (configured and switched on in Global Settings).
- Per-IP rate limiting on login and email-sending identity endpoints.
- Global ProblemDetails exception handler with trace ids.
- Blob storage readiness check and `/healthz/live` liveness endpoint.
- Business metrics (orders, revenue, top-ups, push outcomes, side-effect failures, job durations) and background job spans.
- HSTS and basic security headers; Dependabot configuration.

### Fixed

- Saldo and stock updates are atomic, so concurrent orders for the same user or product no longer lose updates.
- Failures in live updates, achievements or notifications after an order is saved no longer return an error (which invited double charges on retry).
- Oversized images are rejected before decoding.
- Startup no longer runs migrations twice.

## `feature/achievement-rework` - 2026-09-19

### Added

- Introduced Achievement System 2.0 with tiered progression, achievement points, and expanded achievement metadata.
- Added user statistics tracking for product activity, orders, social actions, and top-ups through domain events.
- Added product-user statistic persistence and repository support.
- Added statistics recalculation jobs, including job status reporting and a recalculation timestamp for user stats.
- Added a frontend statistics view with user stat models, tier badges, and leaderboard/profile integration.
- Added administration controls for achievement management and user statistics/recalculation workflows.
- Added database migrations and indexes for achievements, user stats, product-user stats, and DTU query optimization.

### Changed

- Updated achievement, application-user, order, product, notification, quote, reaction, and top-up services to participate in the revised achievement and statistics flows.
- Extended achievement and application-user APIs with revised DTOs, listing data, and recalculation endpoints.
- Added shared domain-event dispatching and achievement event handling for consistent stat and achievement updates.
- Refined the Angular achievement detail, listing, modal, admin, leaderboard, and user-profile experiences.

### Tests

- Updated controller, repository, service, order, and achievement-rule tests for the revised behavior.
- Added coverage for the new achievement, user-stat, event-dispatching, and recalculation pathways.