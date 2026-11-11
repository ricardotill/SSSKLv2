# The SSSKL API as a container: a Release build of SSSKL.Web, run as the non-root `app` user on port 8080.
# Build from the repository root (it needs Directory.Build.props, Directory.Packages.props and the license):
#   docker buildx build -f Backend/Dockerfile --secret id=sixlabors_lic,src=sixlabors.lic --build-arg VERSION=1.2.3 .
# CI (.github/workflows/dotnet.yml) builds it for linux/amd64 and linux/arm64 and pushes it to
# ghcr.io/ricardotill/ssskl-api. Run it with docker-compose.yml or docker-compose.external.yml; see docs/docker.md.

# The build stage always runs on the machine's own platform and cross-compiles for the target one,
# so an arm64 image needs no slow emulation; only the final stage is per architecture.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:11.0 AS build
ARG TARGETARCH
# The app version; CI passes the release tag (1.4.0) or 0.0.0-ci.<commit> for a build of master
ARG VERSION=0.0.0-local
ENV DOTNET_NOLOGO=true DOTNET_CLI_TELEMETRY_OPTOUT=true

WORKDIR /repo
# Restore first, so a code change does not download all packages again
COPY Directory.Build.props Directory.Packages.props ./
COPY Backend/SSSKL.Domain/*.csproj Backend/SSSKL.Domain/
COPY Backend/SSSKL.Contracts/*.csproj Backend/SSSKL.Contracts/
COPY Backend/SSSKL.Application/*.csproj Backend/SSSKL.Application/
COPY Backend/SSSKL.Infrastructure/*.csproj Backend/SSSKL.Infrastructure/
COPY Backend/SSSKL.ServiceDefaults/*.csproj Backend/SSSKL.ServiceDefaults/
COPY Backend/SSSKL.Web/*.csproj Backend/SSSKL.Web/
RUN dotnet restore Backend/SSSKL.Web/SSSKL.Web.csproj -a $TARGETARCH

COPY Backend/ Backend/
# The ImageSharp license is checked at build time; as a secret it never ends up in a layer.
# Sentry symbol upload stays off here (no SENTRY_AUTH_TOKEN); the App Service deploy does that.
RUN --mount=type=secret,id=sixlabors_lic,target=/repo/sixlabors.lic \
    dotnet publish Backend/SSSKL.Web/SSSKL.Web.csproj -c Release -a $TARGETARCH -o /app --no-restore \
      -p:Version=$VERSION -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS final
ARG VERSION=0.0.0-local
LABEL org.opencontainers.image.title="SSSKL API" \
      org.opencontainers.image.source="https://github.com/ricardotill/SSSKLv2" \
      org.opencontainers.image.version=$VERSION
WORKDIR /app
COPY --from=build /app .

# Everything the app writes lives in /data (a volume), owned by `app` so a new volume is writable:
# the data protection keys (logins survive a restart) and, with Storage__Provider=LocalFile, the uploaded files.
RUN mkdir -p /data/keys /data/blobs && chown -R app:app /data

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DataProtection__KeysPath=/data/keys \
    Storage__Provider=LocalFile \
    Storage__LocalRoot=/data/blobs \
    Azure__SignalR__Enabled=false

USER app
EXPOSE 8080
VOLUME ["/data"]
ENTRYPOINT ["dotnet", "SSSKL.Web.dll"]
