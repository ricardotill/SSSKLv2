using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class QuoteRepository(ApplicationDbContext dbContext) : IQuoteRepository
{
    public async Task<(IEnumerable<Quote> Items, int TotalCount)> GetAll(int skip = 0, int take = 15, IList<string>? userRoles = null, bool isAdmin = false, string? targetUserId = null)
    {
        var query = dbContext.Quote
            .Include(q => q.CreatedBy)
            .Include(q => q.Authors)
                .ThenInclude(a => a.ApplicationUser)
            .Include(q => q.VisibleToRoles)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(q => !q.VisibleToRoles.Any() || 
                                     (userRoles != null && q.VisibleToRoles.Any(r => userRoles.Contains(r.Name!))));
        }

        if (!string.IsNullOrEmpty(targetUserId))
        {
            query = query.Where(q => q.Authors.Any(a => a.ApplicationUserId == targetUserId));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(q => q.DateSaid)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Quote?> GetById(Guid id)
    {
        return await dbContext.Quote
            .Include(q => q.CreatedBy)
            .Include(q => q.Authors)
                .ThenInclude(a => a.ApplicationUser)
            .Include(q => q.VisibleToRoles)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task Add(Quote quote)
    {
        await dbContext.Quote.AddAsync(quote);
        await dbContext.SaveChangesAsync();
    }

    public async Task Update(Quote quote)
    {
        dbContext.Quote.Update(quote);
        await dbContext.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        var quote = await dbContext.Quote.FindAsync(id);
        if (quote != null)
        {
            dbContext.Quote.Remove(quote);
            await dbContext.SaveChangesAsync();
        }
    }
}
