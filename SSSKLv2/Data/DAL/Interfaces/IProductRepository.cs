namespace SSSKLv2.Data.DAL.Interfaces;

public interface IProductRepository
{
    public Task<IList<Product>> GetAll();
    public Task<IList<Product>> GetAllAvailable();
    public Task<Product> GetById(Guid id);
    public Task Create(Product obj);
    public Task Update(Product obj);
    public Task Delete(Guid id);
}