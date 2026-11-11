using SSSKL.Domain.Entities;


namespace SSSKL.Contracts.Agents;

public interface IBlobStorageAgent
{
    Task<BlobStorageItem> UploadFileToBlobAsync(string strFileName, string contentType, Stream fileStream);
    Task<bool> DeleteFileToBlobAsync(string strFileName);
    Task<Stream> OpenDownloadStreamAsync(string strFileName);
}