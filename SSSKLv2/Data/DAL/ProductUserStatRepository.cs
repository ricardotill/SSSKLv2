using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class ProductUserStatRepository(ApplicationDbContext context) : IProductUserStatRepository
{
    public async Task<ProductUserStat?> GetByUserAndProduct(string userId, Guid productId)
    {
        return await context.ProductUserStats
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ProductId == productId);
    }

    public async Task<IList<ProductUserStat>> GetByUserId(string userId)
    {
        return await context.ProductUserStats
            .Where(s => s.UserId == userId)
            .ToListAsync();
    }

    public async Task<IList<ProductUserStat>> GetAllForProduct(Guid productId)
    {
        return await context.ProductUserStats
            .Where(s => s.ProductId == productId)
            .Include(s => s.User)
            .ThenInclude(u => u.ProfileImage)
            .ToListAsync();
    }

    public async Task<ProductUserStat> GetOrCreate(string userId, Guid productId)
    {
        var stat = await GetByUserAndProduct(userId, productId);
        if (stat == null)
        {
            stat = new ProductUserStat { UserId = userId, ProductId = productId };
            context.ProductUserStats.Add(stat);
            await context.SaveChangesAsync();
        }
        return stat;
    }

    public async Task<IList<ProductUserStat>> RecalculateByUserId(string userId)
    {
        var orders = await context.Order
            .Include(order => order.Product)
            .Where(order => order.User.Id == userId && order.Product != null)
            .ToListAsync();

        var grouped = orders.GroupBy(order => order.Product!.Id).ToList();

        var existingStats = await context.ProductUserStats
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var result = new List<ProductUserStat>();
        foreach (var group in grouped)
        {
            var stat = existingStats.FirstOrDefault(s => s.ProductId == group.Key);
            if (stat == null)
            {
                stat = new ProductUserStat { UserId = userId, ProductId = group.Key };
                context.ProductUserStats.Add(stat);
            }

            stat.TotalAmount = group.Sum(order => order.Amount);
            stat.TotalOrders = group.Count();
            stat.TotalSpent = group.Sum(order => order.Paid);
            stat.LastOrderDate = group.Max(order => order.CreatedOn);
            result.Add(stat);
        }

        // Drop cached rows for products the user no longer has orders for
        var currentProductIds = grouped.Select(g => g.Key).ToHashSet();
        var staleStats = existingStats.Where(s => !currentProductIds.Contains(s.ProductId));
        context.ProductUserStats.RemoveRange(staleStats);

        await context.SaveChangesAsync();
        return result;
    }

    public async Task Update(ProductUserStat stat)
    {
        context.ProductUserStats.Update(stat);
        await context.SaveChangesAsync();
    }
}
