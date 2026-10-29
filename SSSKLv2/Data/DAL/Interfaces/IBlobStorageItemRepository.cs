namespace SSSKLv2.Data.DAL.Interfaces;

public interface IBlobStorageItemRepository
{
    Task<BlobStorageItem?> GetById(Guid id);
}
