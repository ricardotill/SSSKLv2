# SixLabors ImageSharp License Setup

## Local Development Setup

For local development, you need to have a valid SixLabors license file in the repository root.

### Steps:

1. **Get your license file** from SixLabors
   - If you have a license key, create a file named `sixlabors.lic` in the repository root with the license content

2. **Place the license file** at the repository root:
   ```
   SSSKLv2/
   ├── sixlabors.lic  ← License file goes here
   ├── SSSKLv2/
   ├── .github/
   └── ...
   ```

3. **Build and run** normally:
   ```bash
   dotnet build
   dotnet run
   ```

The `SixLaborsLicenseFile` property in `SSSKLv2.csproj` is configured to look for the license at `../sixlabors.lic` relative to the project directory, which resolves to the repository root and keeps the CI setup consistent.

## Pipeline/CI Setup

The GitHub Actions workflow (`dotnet.yml`) automatically handles license setup:

1. The workflow expects a `SixLaborsLicense` secret containing the **base64-encoded** license file
2. During the build, it decodes the secret and saves it as `./sixlabors.lic` in the repository root
3. The build then proceeds with the license available

### To set up the GitHub secret:

1. Base64 encode your `sixlabors.lic` file:
   ```bash
   base64 -i sixlabors.lic | pbcopy  # macOS
   # or
   base64 sixlabors.lic  # Linux
   # or
   [Convert]::ToBase64String([System.IO.File]::ReadAllBytes('sixlabors.lic')) | Set-Clipboard  # Windows PowerShell
   ```

2. Add it as a GitHub secret named `SixLaborsLicense`:
   - Go to: Settings → Secrets and variables → Actions
   - Click "New repository secret"
   - Name: `SixLaborsLicense`
   - Value: Paste the base64-encoded content
   - Click "Add secret"

## Troubleshooting

If you see: `No Six Labors license found...`

- **Local**: Ensure `sixlabors.lic` exists at the repository root
- **Pipeline**: Verify the `SixLaborsLicense` secret is set and contains base64-encoded license data
