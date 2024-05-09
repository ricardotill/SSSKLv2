using System.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SSSKLv2.Data;

namespace SSSKLv2.Agents;

public class BlobStorageService : IBlobStorageService
{
    private readonly string _blobContainerName;
    private readonly BlobServiceClient _storage;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        IConfiguration configuration, 
        BlobServiceClient storage,
        ILogger<BlobStorageService> logger)
    {
        _blobContainerName = configuration.GetSection("Storage")["ContainerName"] 
                             ?? throw new ConfigurationErrorsException("No Blob Container name found in config");
        _storage = storage;
        _logger = logger;
    }
    
    public async Task<BlobStorageItem> UploadFileToBlobAsync(string strFileName, string contentType, Stream fileStream)
    {
        var container = _storage.GetBlobContainerClient(_blobContainerName);
        var createResponse = await container.CreateIfNotExistsAsync();
        if (createResponse != null && createResponse.GetRawResponse().Status == 201)
            await container.SetAccessPolicyAsync(PublicAccessType.Blob);
        var blob = container.GetBlobClient(strFileName);
        await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        await blob.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });
        var urlString = blob.Uri.ToString();
        return new BlobStorageItem() { FileName = strFileName, Uri = urlString, ContentType = contentType };
    }

    public async Task<bool> DeleteFileToBlobAsync(string strFileName)
    {
        var container = _storage.GetBlobContainerClient(_blobContainerName);
        var createResponse = await container.CreateIfNotExistsAsync();
        if (createResponse != null && createResponse.GetRawResponse().Status == 201)
            await container.SetAccessPolicyAsync(PublicAccessType.Blob);
        var blob = container.GetBlobClient(strFileName);
        await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        return true;
    }
}