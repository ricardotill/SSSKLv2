using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class EventRepository(ApplicationDbContext context, ILogger<EventRepository> logger) : IEventRepository
{
    private readonly ILogger<EventRepository> _logger = logger;

    public async Task<IList<Event>> GetAll(int skip = 0, int take = 15, bool futureOnly = false, IList<string>? userRoles = null, bool isAdmin = false, string? requiredRole = null)
    {
        var query = context.Event
            .Include(e => e.Creator)
            .Include(e => e.Image)
            .Include(e => e.RequiredRoles)
            .Include(e => e.Responses)
                .ThenInclude(r => r.User)
            .AsQueryable();

        var now = DateTime.UtcNow;
        if (futureOnly)
        {
            query = query.Where(e => e.EndDateTime >= now);
        }

        if (!isAdmin)
        {
            if (string.IsNullOrEmpty(requiredRole))
            {
                query = query.Where(e => !e.RequiredRoles.Any() || 
                                         (userRoles != null && e.RequiredRoles.Any(r => userRoles.Contains(r.Name!))));
            }
        }

        if (!string.IsNullOrEmpty(requiredRole))
        {
            query = query.Where(e => e.RequiredRoles.Any(r => r.Name == requiredRole));
        }

        return await query
            .OrderBy(e => e.EndDateTime < now ? 1 : 0) // Groups: Active (0) first
            .ThenBy(e => e.EndDateTime < now ? DateTime.MinValue : e.StartDateTime) // Active: ASC, Past: pinned to Min
            .ThenByDescending(e => e.StartDateTime) // Past: DESC
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetCount(bool futureOnly = false, IList<string>? userRoles = null, bool isAdmin = false, string? requiredRole = null)
    {
        var query = context.Event.AsQueryable();
        if (futureOnly)
        {
            query = query.Where(e => e.EndDateTime >= DateTime.UtcNow);
        }
        
        if (!isAdmin)
        {
            if (string.IsNullOrEmpty(requiredRole))
            {
                query = query.Where(e => !e.RequiredRoles.Any() || 
                                         (userRoles != null && e.RequiredRoles.Any(r => userRoles.Contains(r.Name!))));
            }
        }

        if (!string.IsNullOrEmpty(requiredRole))
        {
            query = query.Where(e => e.RequiredRoles.Any(r => r.Name == requiredRole));
        }
        return await query.CountAsync();
    }

    public async Task<Event?> GetById(Guid id)
    {
        return await context.Event
            .AsNoTracking()
            .Include(e => e.Creator)
            .Include(e => e.Image)
            .Include(e => e.RequiredRoles)
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
        var trackedEntry = context.Entry(entity);
        if (trackedEntry.State != EntityState.Detached)
        {
            _logger.LogWarning(
                "EventRepository.Update received already-tracked entity; detaching before refresh save. EventId={EventId}, EventState={EventState}, ImageState={ImageState}",
                entity.Id,
                trackedEntry.State,
                entity.Image is null ? "null" : context.Entry(entity.Image).State.ToString());

            context.Entry(entity).State = EntityState.Detached;
            if (entity.Image is not null)
            {
                context.Entry(entity.Image).State = EntityState.Detached;
            }

            foreach (var role in entity.RequiredRoles ?? [])
            {
                if (role != null)
                {
                    context.Entry(role).State = EntityState.Detached;
                }
            }
        }

        var existing = await context.Event
            .Include(e => e.Image)
            .Include(e => e.RequiredRoles)
            .AsTracking()
            .SingleOrDefaultAsync(e => e.Id == entity.Id);

        if (existing == null)
            throw new InvalidOperationException($"Event {entity.Id} could not be found for update.");

        context.Entry(existing).CurrentValues.SetValues(entity);

        if (entity.Image is not null)
        {
            if (existing.Image is null)
            {
                existing.Image = entity.Image;
            }
            else
            {
                existing.Image.FileName = entity.Image.FileName;
                existing.Image.Uri = entity.Image.Uri;
                existing.Image.ContentType = entity.Image.ContentType;
                existing.Image.CreatedOn = entity.Image.CreatedOn == default ? DateTime.UtcNow : entity.Image.CreatedOn;
            }
        }
        else if (existing.Image is not null)
        {
            context.EventImage.Remove(existing.Image);
            existing.Image = null;
        }

        if (existing.RequiredRoles is not null)
        {
            existing.RequiredRoles.Clear();
            foreach (var role in entity.RequiredRoles ?? [])
            {
                if (role == null)
                    continue;

                var trackedRole = await context.Roles.FindAsync(role.Id);
                if (trackedRole != null)
                {
                    existing.RequiredRoles.Add(trackedRole);
                    continue;
                }

                existing.RequiredRoles.Add(role);
            }
        }

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
