namespace SSSKLv2.Data.DAL.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    public Task<IList<Product>> GetAll();
}