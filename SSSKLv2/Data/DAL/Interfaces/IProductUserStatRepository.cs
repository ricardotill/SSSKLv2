using SSSKLv2.Data;

namespace SSSKLv2.Data.DAL.Interfaces;

public interface IProductUserStatRepository
{
    Task<ProductUserStat?> GetByUserAndProduct(string userId, Guid productId);
    Task<IList<ProductUserStat>> GetByUserId(string userId);
    Task<IList<ProductUserStat>> GetAllForProduct(Guid productId);
    Task<ProductUserStat> GetOrCreate(string userId, Guid productId);
    Task<IList<ProductUserStat>> RecalculateByUserId(string userId);
    Task Update(ProductUserStat stat);
}
