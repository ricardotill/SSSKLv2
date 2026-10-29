using SSSKL.Domain.Entities;


namespace SSSKL.Contracts.Services;

public interface IProductService
{
    Task<int> GetCount();
    public Task<Product> GetProductById(Guid id);
    public Task<IList<Product>> GetProductsByIds(IEnumerable<Guid> ids);
    public Task<IList<Product>> GetAll();
    public Task<IList<Product>> GetAll(int skip, int take);
    public Task<IList<Product>> GetAllAvailable();
    public Task CreateProduct(Product obj);
    public Task UpdateProduct(Product obj);
    public Task DeleteProduct(Guid id);
}