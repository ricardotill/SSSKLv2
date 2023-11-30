using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class ProductRepository(IDbContextFactory<ApplicationDbContext> _dbContextFactory) : IProductRepository
{
    public async Task<Product> GetById(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Product>> GetAll()
    {
        throw new NotImplementedException();
    }

    public async Task Create(Product obj)
    {
        using (var context = _dbContextFactory.CreateDbContext())
        {
            context.Product.Add(obj);
            await context.SaveChangesAsync();
        }
    }

    public async Task Update(Product obj)
    {
        using (var context = _dbContextFactory.CreateDbContext())
        {
            context.Product.Add(obj);
            await context.SaveChangesAsync();
        }
    }

    public async Task Delete(Guid id)
    {
        using (var context = await _dbContextFactory.CreateDbContextAsync())
        {
            var entry = await context.Product.FindAsync(id);
            if (entry != null)
            {
                context.Product.Remove(entry);
                await context.SaveChangesAsync();
            }
            else throw new NotFoundException("Product Not Found");
        }
    }
}