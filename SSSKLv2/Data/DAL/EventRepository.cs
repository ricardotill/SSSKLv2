using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class EventRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory, ILogger<EventRepository>? logger = null) : IEventRepository
{
    private readonly ILogger<EventRepository>? _logger = logger;

    public async Task<IList<Event>> GetAll(int skip = 0, int take = 15, bool futureOnly = false, IList<string>? userRoles = null, bool isAdmin = false, string? requiredRole = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

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

        if (!isAdmin && string.IsNullOrEmpty(requiredRole))
        {
            query = query.Where(e => !e.RequiredRoles.Any() ||
                                     (userRoles != null && e.RequiredRoles.Any(r => userRoles.Contains(r.Name!))));
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
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var query = context.Event.AsQueryable();
        if (futureOnly)
        {
            query = query.Where(e => e.EndDateTime >= DateTime.UtcNow);
        }

        if (!isAdmin && string.IsNullOrEmpty(requiredRole))
        {
            query = query.Where(e => !e.RequiredRoles.Any() ||
                                     (userRoles != null && e.RequiredRoles.Any(r => userRoles.Contains(r.Name!))));
        }

        if (!string.IsNullOrEmpty(requiredRole))
        {
            query = query.Where(e => e.RequiredRoles.Any(r => r.Name == requiredRole));
        }

        return await query.CountAsync();
    }

    public async Task<Event?> GetById(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

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
        await using var context = await dbContextFactory.CreateDbContextAsync();

        await context.Event.AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public async Task Update(Event entity)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var existing = await context.Event
            .Include(e => e.Image)
            .Include(e => e.RequiredRoles)
            .SingleOrDefaultAsync(e => e.Id == entity.Id);

        if (existing == null)
            throw new InvalidOperationException($"Event {entity.Id} could not be found for update.");

        existing.Title = entity.Title;
        existing.Description = entity.Description;
        existing.StartDateTime = entity.StartDateTime;
        existing.EndDateTime = entity.EndDateTime;
        existing.LocationName = entity.LocationName;
        existing.Latitude = entity.Latitude;
        existing.Longitude = entity.Longitude;

        var incomingRoles = entity.RequiredRoles ?? [];
        existing.RequiredRoles.Clear();

        if (incomingRoles.Count > 0)
        {
            foreach (var role in incomingRoles)
            {
                if (role == null)
                    continue;

                var trackedRole = await context.Roles
                    .FirstOrDefaultAsync(r => r.Id == role.Id || r.Name == role.Name);

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

    public async Task UpdateImage(Guid eventId, EventImage image)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var existingEvent = await context.Event
            .Include(e => e.Image)
            .SingleOrDefaultAsync(e => e.Id == eventId);

        if (existingEvent == null)
            throw new InvalidOperationException($"Event {eventId} could not be found for image update.");

        if (existingEvent.Image is null)
        {
            image.Event = existingEvent;
            existingEvent.Image = image;
            context.EventImage.Add(image);
        }
        else
        {
            var existingImage = existingEvent.Image;
            var replacementImage = new EventImage
            {
                FileName = image.FileName,
                Uri = image.Uri,
                ContentType = image.ContentType,
                CreatedOn = image.CreatedOn == default ? DateTime.UtcNow : image.CreatedOn,
                Event = existingEvent
            };

            existingEvent.Image = replacementImage;
            context.EventImage.Add(replacementImage);

            if (existingImage.Id != Guid.Empty)
            {
                context.EventImage.Remove(existingImage);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var entity = await context.Event.FindAsync(id);
        if (entity != null)
        {
            context.Event.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task<EventResponse?> GetResponse(Guid eventId, string userId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.EventResponse
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
    }

    public async Task AddResponse(EventResponse response)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        await context.EventResponse.AddAsync(response);
        await context.SaveChangesAsync();
    }

    public async Task UpdateResponse(EventResponse response)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.EventResponse.Update(response);
        await context.SaveChangesAsync();
    }

    public async Task DeleteResponse(EventResponse response)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.EventResponse.Remove(response);
        await context.SaveChangesAsync();
    }
}
