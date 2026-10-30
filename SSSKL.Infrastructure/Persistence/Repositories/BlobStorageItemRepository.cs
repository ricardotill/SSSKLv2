using Microsoft.EntityFrameworkCore;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Infrastructure.Persistence.Repositories;

public class BlobStorageItemRepository(ApplicationDbContext dbContext) : IBlobStorageItemRepository
{
    public async Task<BlobStorageItem?> GetById(Guid id)
    {
        return await dbContext.BlobStorageItem.FirstOrDefaultAsync(x => x.Id == id);
    }
}
