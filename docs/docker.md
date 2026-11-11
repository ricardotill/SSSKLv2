# Docker and releases

SSSKL ships as two Docker images, which CI builds for `linux/amd64` and `linux/arm64` and pushes to the GitHub Container Registry:

| Image | Built from | Contents |
| :--- | :--- | :--- |
| `ghcr.io/ricardotill/ssskl-api` | `Backend/Dockerfile` | The API (`SSSKL.Web`), port 8080, as the non-root `app` user. |
| `ghcr.io/ricardotill/ssskl-frontend` | `Frontend/Dockerfile` | The Angular build, served by Caddy on port 8080 (`Frontend/Caddyfile`). |

Caddy in the frontend image passes `/api/*` and `/hubs/*` (SignalR, including WebSockets) on to the `api` container. The browser only talks to the frontend container, so the SPA and the API share one origin and need no CORS.

The Azure deploys (App Service for the API, Static Web Apps for the SPA) stay in place next to the images.

## Releases

The git tag is the only place where the version is set. `Directory.Build.props` and `Frontend/package.json` stay at `0.0.0`. To release:

```bash
git checkout master && git pull
git tag v3.13.0
git push origin v3.13.0
```

Each workflow gets the version from the tag via `.github/workflows/version.yml`. It ends up in:
- the API assembly (`-p:Version`): `/api/v1/public/version` and the Sentry release `SSSKL.Web@3.13.0`;
- the SPA (`APP_VERSION`): `CURRENT_APP_VERSION` and the Sentry release `ssskl-frontend@3.13.0`.

| Push | Azure (App Service and Static Web Apps) | Docker image tags |
| :--- | :--- | :--- |
| a commit to `master` | nothing | `master`, `sha-<commit>` |
| tag `v3.13.0` on master | deploys 3.13.0 | `3.13.0`, `3.13`, `latest` |
| tag `v3.13.0-rc.1` on master | nothing | `3.13.0-rc.1` |
| a tag that is not on master | nothing (the version job fails) | nothing |
| a pull request | Static Web Apps preview | built, not pushed |

An image is only pushed after its tests pass: Backend CI (`dotnet.yml`) for the API, and Frontend CI (`frontend-ci.yml`, unit and E2E tests) for the frontend. A server that follows `latest` therefore only gets releases.

To redeploy a release to App Service, run "Build and deploy ASP.Net Core app to Azure Web App" by hand (Actions → Run workflow) and pick the tag, not a branch.

### One-time setup for tag deploys
- **Azure login for App Service:** the deploy job runs in the GitHub environment `production`, because the subject of a tag run changes with every tag.
  1. In Azure, open the app registration or managed identity behind `AZUREAPPSERVICE_CLIENTID_…`.
  2. Under *Federated credentials*, add a GitHub credential with entity type **Environment** and name `production`. The subject is `repo:ricardotill/SSSKLv2:environment:production`.
  3. Optionally, limit the `production` environment in GitHub (Settings → Environments) to tags matching `v*`.
- **GHCR:** the first push creates the packages `ssskl-api` and `ssskl-frontend`. They are private by default. Make them public, or log in on the server with `docker login ghcr.io` and a personal access token with `read:packages`.

## Running it

There are two compose files at the repository root. Neither mounts anything from the repository, so both also work as a Portainer stack.

| File | Runs | For |
| :--- | :--- | :--- |
| `docker-compose.yml` | SQL Server, API, frontend; uploads as files in a volume | Everything on one machine |
| `docker-compose.external.yml` | API, frontend | An existing database and blob storage (Azure SQL, Azure Storage, your own SQL Server) |

```bash
cp .env.example .env        # fill it in; see the comments in the file
docker compose pull && docker compose up -d
# or, with your own database and storage:
docker compose -f docker-compose.external.yml pull && docker compose -f docker-compose.external.yml up -d
```

The app is at `http://<host>:8080` (`PORT` in `.env`). The API creates and migrates the database on startup. The data protection keys live in the `api-data` volume, so logins survive a restart or an update.

**Uploaded files**
- The image stores uploads (profile pictures, event and achievement images) as plain files: `Storage__Provider=LocalFile`, in `/data/blobs`.
- In `docker-compose.yml` that folder is in the `api-data` volume. Back up that volume together with the `sql-data` volume, because the database refers to these files.
- `docker-compose.external.yml` sets `Storage__Provider=Azure` and uses Azure Blob Storage through `BLOB_CONNECTION_STRING`.
- The API always streams the files to the browser itself, so the storage never has to be reachable from outside.

**HTTPS**
- The API runs as Production, so its cookies are Secure-only. Put HTTPS in front of it, using one of these:
  - a Cloudflare Tunnel: set `CLOUDFLARE_TUNNEL_TOKEN`, run with `--profile tunnel`, and point the public hostname to `http://frontend:8080`;
  - your own reverse proxy that sends `X-Forwarded-Proto`.
- To try it out on plain `http://localhost`, set `ALLOW_HTTP=true`.
- Set `WEBSITE_DOMAIN` to the host name visitors use; passkeys only work on that name.

**After the first start**
- Register an account. It needs a confirmed email address before it can log in, so first set up the mail server under the admin settings, or confirm the account in the database.
- The SMTP, Turnstile and other settings are kept in the database and edited in the app's admin settings, not in `.env`.

**Updating:** `docker compose pull && docker compose up -d`. Pin a version with `APP_VERSION=3.13.0` in `.env`.

**SignalR:** the API serves its own SignalR hub (`Azure__SignalR__Enabled=false`). That is enough for one API container. With more instances, use Azure SignalR: set `AZURE_SIGNALR_ENABLED=true` and `AZURE_SIGNALR_CONNECTION_STRING` (external file).

## Building the images yourself

Run these from the repository root; it is the build context, because both images need the root `Directory.Build.props`.

```bash
# API: the ImageSharp license is checked at build time and passed as a secret, never stored in a layer
docker buildx build -f Backend/Dockerfile --secret id=sixlabors_lic,src=sixlabors.lic \
  --build-arg VERSION=0.0.0-local -t ghcr.io/ricardotill/ssskl-api:local --load .

# Frontend: both secrets are optional (without the PrimeUI key, PrimeUI shows its license banner)
PRIMEUI_LICENSE_KEY=... docker buildx build -f Frontend/Dockerfile \
  --secret id=primeui_license_key,env=PRIMEUI_LICENSE_KEY \
  --build-arg VERSION=0.0.0-local -t ghcr.io/ricardotill/ssskl-frontend:local --load .
```

Then run them with `APP_VERSION=local` in `.env`.
