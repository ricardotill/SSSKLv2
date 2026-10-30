using Microsoft.EntityFrameworkCore;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Infrastructure.Persistence.Repositories;

public class QuoteRepository(ApplicationDbContext dbContext) : IQuoteRepository
{
    public async Task<(IEnumerable<Quote> Items, int TotalCount)> GetAll(int skip = 0, int take = 15, IList<string>? userRoles = null, bool isAdmin = false, string? targetUserId = null)
    {
        var query = dbContext.Quote
            .Include(q => q.CreatedBy)
            .Include(q => q.Authors)
                .ThenInclude(a => a.ApplicationUser)
            .Include(q => q.VisibleToRoles)
            .AsSplitQuery()
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
            .ThenBy(q => q.Id)
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
            .AsSplitQuery()
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

    public async Task<IDictionary<Guid, QuoteVoteSummary>> GetVoteSummaries(IEnumerable<Guid> quoteIds, string? userId)
    {
        var ids = quoteIds.ToList();
        return await dbContext.QuoteVote
            .Where(v => ids.Contains(v.QuoteId))
            .GroupBy(v => v.QuoteId)
            .Select(g => new
            {
                QuoteId = g.Key,
                Count = g.Count(),
                HasVoted = userId != null && g.Any(v => v.UserId == userId)
            })
            .ToDictionaryAsync(x => x.QuoteId, x => new QuoteVoteSummary(x.Count, x.HasVoted));
    }

    public async Task<QuoteVote?> GetVote(Guid quoteId, string userId)
    {
        return await dbContext.QuoteVote
            .FirstOrDefaultAsync(v => v.QuoteId == quoteId && v.UserId == userId);
    }

    public async Task AddVote(QuoteVote vote)
    {
        dbContext.QuoteVote.Add(vote);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteVote(QuoteVote vote)
    {
        dbContext.QuoteVote.Remove(vote);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IList<string>> GetAuthorUserIds(Guid quoteId)
    {
        return await dbContext.QuoteAuthor
            .Where(author => author.QuoteId == quoteId && author.ApplicationUserId != null)
            .Select(author => author.ApplicationUserId!)
            .Distinct()
            .ToListAsync();
    }
}
