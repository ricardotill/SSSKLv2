using SSSKLv2.Data;

namespace SSSKLv2.Agents;

public interface IBlobStorageService
{
    Task<BlobStorageItem> UploadFileToBlobAsync(string strFileName, string contentType, Stream fileStream);
    Task<bool> DeleteFileToBlobAsync(string strFileName);
}