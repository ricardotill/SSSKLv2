using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository productRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository productRepository, ILogger<ProductService> logger)
    {
        this.productRepository = productRepository;
        _logger = logger;
    }
    
    public Task<int> GetCount() => productRepository.GetCount();

    public async Task<Product> GetProductById(Guid id)
    {
        _logger.LogInformation("{Type}: Get Product with ID {Id}", GetType(), id);
        return await productRepository.GetById(id);
    }

    public async Task<IList<Product>> GetAll()
    {
        _logger.LogInformation("{Type}: Get All Products", GetType());
        return await productRepository.GetAll();
    }
    
    public async Task<IList<Product>> GetAll(int skip, int take)
    {
        _logger.LogInformation("{Type}: Get All Products skip={Skip} take={Take}", GetType(), skip, take);
        return await productRepository.GetAll();
    }
    
    public async Task<IList<Product>> GetAllAvailable()
    {
        _logger.LogInformation("{Type}: Get All Available Products", GetType());
        return await productRepository.GetAllAvailable();
    }

    public async Task CreateProduct(Product obj)
    {
        _logger.LogInformation("{Type}: Get Product with name {Name}", GetType(), obj.Name);
        await productRepository.Create(obj);
    }
    
    public async Task UpdateProduct(Product obj)
    {
        _logger.LogInformation("{Type}: Update Product with ID {Id}", GetType(), obj.Id);
        await productRepository.Update(obj);
    }
    
    public async Task DeleteProduct(Guid id)
    {
        _logger.LogInformation("{Type}: Delete Product with ID {Id}", GetType(), id);
        await productRepository.Delete(id);
    }
}