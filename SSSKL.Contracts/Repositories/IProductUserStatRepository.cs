using SSSKL.Domain.Entities;


namespace SSSKL.Contracts.Repositories;

public interface IProductUserStatRepository
{
    Task<ProductUserStat?> GetByUserAndProduct(string userId, Guid productId);
    Task<IList<ProductUserStat>> GetByUserId(string userId);
    Task<IList<ProductUserStat>> GetAllForProduct(Guid productId);
    Task<ProductUserStat> GetOrCreate(string userId, Guid productId);
    Task<IList<ProductUserStat>> RecalculateByUserId(string userId);
    Task Update(ProductUserStat stat);
}
