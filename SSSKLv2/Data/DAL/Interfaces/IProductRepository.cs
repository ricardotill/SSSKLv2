namespace SSSKLv2.Data.DAL.Interfaces;

public interface IProductRepository
{
    Task<int> GetCount();
    Task<IList<Product>> GetAll();
    Task<IList<Product>> GetAll(int skip, int take);
    Task<IList<Product>> GetAllAvailable();
    Task<Product> GetById(Guid id);
    Task Create(Product obj);
    Task Update(Product obj);
    Task Delete(Guid id);
}