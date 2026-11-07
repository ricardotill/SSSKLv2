using Microsoft.Extensions.Hosting;
using SSSKL.Contracts.Agents;
using SSSKL.Domain.Entities;

namespace SSSKL.Infrastructure.Agents;

/// <summary>
/// Stores blobs on the local disk. Only registered in the E2E environment, which has no Azure Storage.
/// The root folder comes from <c>E2E:BlobRoot</c> (relative paths resolve against the content root).
/// </summary>
public sealed class LocalFileBlobStorageAgent : IBlobStorageAgent
{
    private readonly string _root;

    public LocalFileBlobStorageAgent(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["E2E:BlobRoot"] ?? "e2e-blobs";
        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));
        Directory.CreateDirectory(_root);
    }

    public async Task<BlobStorageItem> UploadFileToBlobAsync(string strFileName, string contentType, Stream fileStream)
    {
        var path = ResolvePath(strFileName);
        await using (var file = File.Create(path))
        {
            await fileStream.CopyToAsync(file);
        }

        var normalizedContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        return new BlobStorageItem { FileName = strFileName, Uri = new Uri(path).AbsoluteUri, ContentType = normalizedContentType };
    }

    public Task<bool> DeleteFileToBlobAsync(string strFileName)
    {
        var path = ResolvePath(strFileName);
        if (File.Exists(path)) File.Delete(path);
        return Task.FromResult(true);
    }

    public Task<Stream> OpenDownloadStreamAsync(string strFileName)
    {
        Stream stream = File.OpenRead(ResolvePath(strFileName));
        return Task.FromResult(stream);
    }

    // Keep every blob inside the root folder, whatever the file name contains.
    private string ResolvePath(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(_root, fileName));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Invalid blob file name.", nameof(fileName));
        return path;
    }
}
