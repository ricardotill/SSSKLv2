# The SSSKL web app (Angular SPA) as a container: the production build, served by Caddy on port 8080.
# Caddy also passes /api and /hubs on to the API container, so the SPA and the API share one origin.
# Build from the repository root (sentry-release.js reads ../Directory.Build.props):
#   docker buildx build -f Frontend/Dockerfile --build-arg VERSION=1.2.3 .
# Optional build secrets: --secret id=primeui_license_key,env=PRIMEUI_LICENSE_KEY (without it PrimeUI shows its
# license banner) and --secret id=sentry_auth_token,env=SENTRY_AUTH_TOKEN (uploads source maps to Sentry).
# CI (.github/workflows/frontend-ci.yml) builds it for linux/amd64 and linux/arm64 and pushes it to
# ghcr.io/ricardotill/ssskl-frontend. See docs/docker.md.

# The build output is plain JS/CSS, the same for every architecture: build it once, on the machine's own platform
FROM --platform=$BUILDPLATFORM node:26-slim AS build
# The app version; CI passes the release tag (1.4.0) or 0.0.0-ci.<commit> for a build of master
ARG VERSION=0.0.0-local
WORKDIR /repo
COPY Directory.Build.props ./

WORKDIR /repo/Frontend
# Install first, so a code change does not download all packages again
COPY Frontend/package.json Frontend/package-lock.json ./
RUN npm ci --ignore-scripts --no-audit --no-fund

COPY Frontend/ ./
RUN --mount=type=secret,id=primeui_license_key,env=PRIMEUI_LICENSE_KEY \
    --mount=type=secret,id=sentry_auth_token,env=SENTRY_AUTH_TOKEN \
    APP_VERSION=$VERSION npm run build

FROM caddy:2.10 AS final
ARG VERSION=0.0.0-local
LABEL org.opencontainers.image.title="SSSKL web app" \
      org.opencontainers.image.source="https://github.com/ricardotill/SSSKLv2" \
      org.opencontainers.image.version=$VERSION
COPY Frontend/Caddyfile /etc/caddy/Caddyfile
COPY --from=build /repo/Frontend/dist/SSSKL/browser /srv

# Where Caddy sends /api and /hubs: the API container (the service name in the compose files)
ENV API_UPSTREAM=api:8080
EXPOSE 8080
