using System.Collections.Concurrent;
using SSSKL.Contracts.Agents;
using SSSKL.Domain.Entities;

namespace SSSKL.Web.Test.Integration.Infrastructure.Fakes;

/// <summary>Keeps uploaded blobs in memory instead of Azure Storage.</summary>
public sealed class InMemoryBlobStorageAgent : IBlobStorageAgent
{
    private readonly ConcurrentDictionary<string, byte[]> _blobs = new();

    public IReadOnlyDictionary<string, byte[]> Blobs => _blobs;

    public async Task<BlobStorageItem> UploadFileToBlobAsync(string strFileName, string contentType, Stream fileStream)
    {
        using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer);
        _blobs[strFileName] = buffer.ToArray();
        return new BlobStorageItem { FileName = strFileName, Uri = $"/fake/{strFileName}", ContentType = contentType };
    }

    public Task<bool> DeleteFileToBlobAsync(string strFileName)
    {
        _blobs.TryRemove(strFileName, out _);
        return Task.FromResult(true);
    }

    public Task<Stream> OpenDownloadStreamAsync(string strFileName)
    {
        if (!_blobs.TryGetValue(strFileName, out var bytes))
            throw new FileNotFoundException($"Blob '{strFileName}' not found.");
        return Task.FromResult<Stream>(new MemoryStream(bytes, writable: false));
    }
}
