# MediatR License Setup

The backend uses MediatR 14 for its commands, queries and domain events. Since version 13, MediatR is commercial software from Lucky Penny Software. Organizations with less than $5M annual gross revenue can use the free **Community** license. Scouting Wilo qualifies, but MediatR still needs the license key to know that.

The key is read at startup from the configuration value `MediatR:LicenseKey` (see `SSSKL.Application/DependencyInjection.cs`). If the key is missing or invalid, MediatR still works but logs a license warning. A missing key never breaks the build, the tests or the app.

## Get a license key

1. Go to <https://luckypennysoftware.com> and request a **Community** license for MediatR.
2. Register with the organization details (Scouting Wilo) and an email address that the board can access, so the key can be renewed later.
3. You receive a license key: one long string (a signed token). Store it in the team password manager.

The key is not a password, but treat it like configuration that belongs to the organization: don't commit it to the repository.

## Local development

`SSSKL.Web` has a `UserSecretsId`, so store the key with user-secrets:

```bash
dotnet user-secrets set "MediatR:LicenseKey" "<your-license-key>" --project SSSKL.Web
```

To check that the key is stored:

```bash
dotnet user-secrets list --project SSSKL.Web
```

User-secrets are loaded in the Development environment, so the key also applies when you start the stack with `dotnet run --project SSSKL.AppHost`.

## Production (Azure App Service)

Set the key as an application setting on the `sssklv2-app` App Service. In App Service settings, `__` (double underscore) replaces `:`.

### Azure Portal

1. Open the App Service **sssklv2-app**.
2. Go to **Settings → Environment variables → App settings**.
3. Click **Add**:
   - Name: `MediatR__LicenseKey`
   - Value: the license key
   - Leave **Deployment slot setting** unchecked
4. Click **Apply**, then **Confirm**. The app restarts and loads the key.

### Azure CLI

```bash
az webapp config appsettings set \
  --name sssklv2-app \
  --resource-group <resource-group> \
  --settings MediatR__LicenseKey="<your-license-key>"
```

The GitHub Actions deploy (`master_sssklv2-app.yml`) only deploys the build. It doesn't touch app settings, so the key stays in place across deploys.

## CI and tests

No setup is needed. Unit tests create handlers directly, and the integration tests (`WebApplicationFactory`) run without a key; MediatR only logs a warning.

## Verify

After the app starts, look for a MediatR license message in the logs:

- **Locally**: check the console output of `SSSKL.Web` or the structured logs in the Aspire dashboard.
- **Production**: run this query in Application Insights (**Logs**):

  ```kusto
  traces
  | where timestamp > ago(1d)
  | where message has "MediatR" and message has "licens"
  | project timestamp, severityLevel, message
  | order by timestamp desc
  ```

  No rows after the latest restart means that the key was accepted.

## Renewal

Community licenses have an expiry date. When the key expires, MediatR keeps working but logs a license warning again. Request a new key, then replace the value in user-secrets and in the App Service setting, as described above.

## Troubleshooting

If you see a MediatR license warning:

- **Locally**: run `dotnet user-secrets list --project SSSKL.Web` and check that `MediatR:LicenseKey` is there. Also check that you run in the `Development` environment, because user-secrets aren't loaded otherwise.
- **Production**: check that the app setting is named `MediatR__LicenseKey`, with two underscores, and that the value contains no quotes or extra spaces.
- **Expired key**: request a new key (see [Renewal](#renewal)).
