using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class ProductService(
    IProductRepository _productRepository,
    ILogger<ProductService> _logger) : IProductService
{
    public async Task<Product> GetProductById(Guid id)
    {
        _logger.LogInformation($"{GetType()}: Get Product with ID {id}");
        return await _productRepository.GetById(id);
    }

    public async Task<IList<Product>> GetAll()
    {
        _logger.LogInformation($"{GetType()}: Get All Products");
        return await _productRepository.GetAll();
    }

    public async Task CreateProduct(Product obj)
    {
        _logger.LogInformation($"{GetType()}: Get Product with name {obj.Name}");
        await _productRepository.Create(obj);
    }
    
    public async Task UpdateProduct(Product obj)
    {
        _logger.LogInformation($"{GetType()}: Update Product with ID {obj.Id}");
        await _productRepository.Update(obj);
    }
    
    public async Task DeleteProduct(Guid id)
    {
        _logger.LogInformation($"{GetType()}: Delete Product with ID {id}");
        await _productRepository.Delete(id);
    }
}