using SSSKL.Domain.Entities;

namespace SSSKL.Contracts.Repositories;

public interface IBlobStorageItemRepository
{
    Task<BlobStorageItem?> GetById(Guid id);
}
