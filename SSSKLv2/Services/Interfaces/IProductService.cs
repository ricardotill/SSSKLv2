using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface IProductService
{
    Task<int> GetCount();
    public Task<Product> GetProductById(Guid id);
    public Task<IList<Product>> GetAll();
    public Task<IList<Product>> GetAll(int skip, int take);
    public Task<IList<Product>> GetAllAvailable();
    public Task CreateProduct(Product obj);
    public Task UpdateProduct(Product obj);
    public Task DeleteProduct(Guid id);
}