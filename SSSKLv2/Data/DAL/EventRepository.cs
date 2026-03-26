using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class EventRepository(ApplicationDbContext context) : IEventRepository
{
    public async Task<IList<Event>> GetAll(int skip = 0, int take = 15, bool futureOnly = false)
    {
        var query = context.Event
            .Include(e => e.Creator)
            .Include(e => e.Image)
            .Include(e => e.Responses)
                .ThenInclude(r => r.User)
            .AsQueryable();

        var now = DateTime.Now;
        if (futureOnly)
        {
            query = query.Where(e => e.EndDateTime >= now);
        }

        return await query
            .OrderBy(e => e.EndDateTime < now)
            .ThenBy(e => e.StartDateTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetCount(bool futureOnly = false)
    {
        var query = context.Event.AsQueryable();
        if (futureOnly)
        {
            query = query.Where(e => e.EndDateTime >= DateTime.Now);
        }
        return await query.CountAsync();
    }

    public async Task<Event?> GetById(Guid id)
    {
        return await context.Event
            .Include(e => e.Creator)
            .Include(e => e.Image)
            .Include(e => e.Responses)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task Add(Event entity)
    {
        await context.Event.AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public async Task Update(Event entity)
    {
        context.Event.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        var entity = await context.Event.FindAsync(id);
        if (entity != null)
        {
            context.Event.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task<EventResponse?> GetResponse(Guid eventId, string userId)
    {
        return await context.EventResponse
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
    }

    public async Task AddResponse(EventResponse response)
    {
        await context.EventResponse.AddAsync(response);
        await context.SaveChangesAsync();
    }

    public async Task UpdateResponse(EventResponse response)
    {
        context.EventResponse.Update(response);
        await context.SaveChangesAsync();
    }

    public async Task DeleteResponse(EventResponse response)
    {
        context.EventResponse.Remove(response);
        await context.SaveChangesAsync();
    }
}
