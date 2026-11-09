using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SSSKL.Infrastructure.Hosting;

namespace SSSKL.Infrastructure.Agents;

/// <summary>
/// Where uploaded files are stored, from <c>Storage:Provider</c>:
/// <c>Azure</c> (Azure Blob Storage, <see cref="BlobStorageAgent"/>) or
/// <c>LocalFile</c> (a folder on disk, <see cref="LocalFileBlobStorageAgent"/>, used by the Docker image and E2E).
/// Without a value the E2E environment uses local files and every other environment Azure.
/// </summary>
internal static class BlobStorageProvider
{
    public const string ConfigurationKey = "Storage:Provider";
    public const string Azure = "Azure";
    public const string LocalFile = "LocalFile";

    public static bool UsesLocalFiles(IConfiguration configuration, IHostEnvironment environment)
    {
        var provider = configuration[ConfigurationKey];

        if (string.IsNullOrWhiteSpace(provider))
            return environment.IsE2E();
        if (string.Equals(provider, LocalFile, StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(provider, Azure, StringComparison.OrdinalIgnoreCase))
            return false;

        throw new InvalidOperationException(
            $"Unknown {ConfigurationKey} '{provider}'. Use '{Azure}' or '{LocalFile}'.");
    }
}
