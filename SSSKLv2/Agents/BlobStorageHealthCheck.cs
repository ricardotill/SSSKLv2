using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SSSKLv2.Agents;

/// <summary>
/// Readiness probe for the blob container used for images. Checks the container the app actually uses,
/// so a missing container or a revoked managed identity role shows up, not just account reachability.
/// </summary>
public class BlobStorageHealthCheck(BlobServiceClient storage, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var containerName = configuration.GetSection("Storage")["ContainerName"];
        if (string.IsNullOrWhiteSpace(containerName))
        {
            return HealthCheckResult.Unhealthy("Storage:ContainerName is not configured");
        }

        try
        {
            var exists = await storage.GetBlobContainerClient(containerName).ExistsAsync(cancellationToken);
            return exists.Value
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Blob container '{containerName}' does not exist");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Blob storage is unreachable", ex);
        }
    }
}
