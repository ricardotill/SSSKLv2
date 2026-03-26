using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class ProductRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IProductRepository
{
    public async Task<int> GetCount()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Product.CountAsync();
    }

    public async Task<Product> GetById(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var entry = await context.Product.FindAsync(id);
        if (entry != null)
        {
            return entry;
        }

        throw new NotFoundException("Product not found");
    }

    public async Task<IList<Product>> GetAll()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var list = await context.Product
            .OrderByDescending(x => x.Orders.Count)
            .ToListAsync();
        return list;
    }
    
    public async Task<IList<Product>> GetAll(int skip, int take)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var list = await context.Product
            .OrderByDescending(x => x.Orders.Count)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return list;
    }
    
    public async Task<IList<Product>> GetAllAvailable()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var list = await context.Product
            .Where(x => x.Stock > 0)
            .OrderByDescending(x => x.Orders.Count)
            .ToListAsync();
        return list;
    }

    public async Task Create(Product obj)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Product.Add(obj);
        await context.SaveChangesAsync();
    }

    public async Task Update(Product obj)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Product.Update(obj);
        await context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var entry = await context.Product.FindAsync(id);
        if (entry != null)
        {
            context.Product.Remove(entry);
            await context.SaveChangesAsync();
        }
        else throw new NotFoundException("Product Not Found");
    }
}