using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class ProductService(IProductRepository _productRepository) : IProductService
{
    public async Task<Product> GetProductById(Guid id)
    {
        return await _productRepository.GetById(id);
    }

    public async Task<IEnumerable<Product>> GetAll()
    {
        return await _productRepository.GetAll();
    }

    public async Task CreateProduct(Product obj)
    {
        await _productRepository.Create(obj);
    }
    
    public async Task UpdateProduct(Product obj)
    {
        await _productRepository.Update(obj);
    }
    
    public async Task CreateProduct(Guid id)
    {
        await _productRepository.Delete(id);
    }
}