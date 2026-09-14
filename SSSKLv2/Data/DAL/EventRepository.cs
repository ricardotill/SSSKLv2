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
        var eventState = trackedEntry.State;
        var imageState = entity.Image is null ? "null" : context.Entry(entity.Image).State.ToString();
        var requiredRoleCount = entity.RequiredRoles?.Count ?? 0;

        _logger.LogInformation(
            "EventRepository.Update start: EventId={EventId}, EventState={EventState}, ImageState={ImageState}, RequiredRoleCount={RequiredRoleCount}, ImageId={ImageId}, ImageNull={ImageNull}",
            entity.Id,
            eventState,
            imageState,
            requiredRoleCount,
            entity.Image?.Id,
            entity.Image is null);

        if (trackedEntry.State != EntityState.Detached)
        {
            _logger.LogInformation(
                "EventRepository.Update skipping reattach because Event already tracked: EventId={EventId}, State={State}",
                entity.Id,
                trackedEntry.State);
            await context.SaveChangesAsync();
            return;
        }

        var existing = await context.Event
            .Include(e => e.Image)
            .Include(e => e.RequiredRoles)
            .AsTracking()
            .SingleOrDefaultAsync(e => e.Id == entity.Id);

        if (existing == null)
        {
            _logger.LogError("EventRepository.Update failed: Event {EventId} not found for update.", entity.Id);
            throw new InvalidOperationException($"Event {entity.Id} could not be found for update.");
        }

        var existingEntry = context.Entry(existing);
        var existingImageState = existing.Image is null ? "null" : context.Entry(existing.Image).State.ToString();

        _logger.LogInformation(
            "EventRepository.Update existing tracked state: EventId={EventId}, ExistingState={ExistingState}, ExistingImageState={ExistingImageState}, ExistingImageId={ExistingImageId}, ExistingRoleCount={ExistingRoleCount}",
            existing.Id,
            existingEntry.State,
            existingImageState,
            existing.Image?.Id,
            existing.RequiredRoles?.Count ?? 0);

        context.Entry(existing).CurrentValues.SetValues(entity);

        if (entity.Image is not null)
        {
            if (existing.Image is null)
            {
                existing.Image = entity.Image;
                _logger.LogInformation("EventRepository.Update: assigned new image to existing event. EventId={EventId}, ImageId={ImageId}", existing.Id, entity.Image.Id);
            }
            else
            {
                existing.Image.FileName = entity.Image.FileName;
                existing.Image.Uri = entity.Image.Uri;
                existing.Image.ContentType = entity.Image.ContentType;
                existing.Image.CreatedOn = entity.Image.CreatedOn == default ? DateTime.UtcNow : entity.Image.CreatedOn;
                _logger.LogInformation("EventRepository.Update: updated existing image metadata. EventId={EventId}, ImageId={ImageId}", existing.Id, existing.Image.Id);
            }
        }
        else if (existing.Image is not null)
        {
            context.EventImage.Remove(existing.Image);
            existing.Image = null;
            _logger.LogInformation("EventRepository.Update: removed existing image. EventId={EventId}, ImageId={ImageId}", existing.Id, existing.Image?.Id);
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

            _logger.LogInformation("EventRepository.Update: refreshed required roles. EventId={EventId}, FinalRoleCount={RoleCount}", existing.Id, existing.RequiredRoles.Count);
        }

        _logger.LogInformation(
            "EventRepository.Update before save: EventId={EventId}, EventState={EventState}, ImageState={ImageState}, RequiredRoles={RequiredRoleCount}",
            existing.Id,
            context.Entry(existing).State,
            existing.Image is null ? "null" : context.Entry(existing.Image).State.ToString(),
            existing.RequiredRoles?.Count ?? 0);

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
