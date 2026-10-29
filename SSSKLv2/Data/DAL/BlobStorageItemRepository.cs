using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class BlobStorageItemRepository(ApplicationDbContext dbContext) : IBlobStorageItemRepository
{
    public async Task<BlobStorageItem?> GetById(Guid id)
    {
        return await dbContext.BlobStorageItem.FirstOrDefaultAsync(x => x.Id == id);
    }
}
