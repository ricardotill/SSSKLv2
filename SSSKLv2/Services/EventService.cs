using Microsoft.EntityFrameworkCore;
using SSSKLv2.Agents;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Dto.Api;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Util;
using SSSKLv2.Data.Constants;
using Ganss.Xss;

namespace SSSKLv2.Services;

public class EventService(IEventRepository eventRepository, IBlobStorageAgent blobStorageAgent, IApplicationUserService applicationUserService, IDbContextFactory<ApplicationDbContext> dbContextFactory, IEventNotifier eventNotifier) : IEventService
{
    private const string EventNotFoundMessage = "Event not found";

    public async Task<IEnumerable<EventDto>> GetAllEvents(int skip = 0, int take = 15, bool futureOnly = false, string? userId = null, string? requiredRole = null)
    {
        var (userRoles, isAdmin) = await GetUserAccessAsync(userId);
        var events = await eventRepository.GetAll(skip, take, futureOnly, userRoles, isAdmin, requiredRole);
        return events.Select(e => MapToDto(e, userId));
    }

    public async Task<int> GetCount(bool futureOnly = false, string? userId = null, string? requiredRole = null)
    {
        var (userRoles, isAdmin) = await GetUserAccessAsync(userId);
        return await eventRepository.GetCount(futureOnly, userRoles, isAdmin, requiredRole);
    }

    public async Task<EventDto> GetEventById(Guid id, string? userId = null)
    {
        var e = await eventRepository.GetById(id);
        if (e == null) throw new Data.DAL.Exceptions.NotFoundException(EventNotFoundMessage);
        return MapToDto(e, userId);
    }

    public async Task<Guid> CreateEvent(EventCreateDto dto, string creatorId)
    {
        var e = BuildEvent(dto, creatorId);
        await ApplyRequiredRolesAsync(e, dto.RequiredRoles);
        await ApplyImageAsync(e, dto);

        await eventRepository.Add(e);
        await eventNotifier.NotifyEventChangedAsync();
        return e.Id;
    }

    public async Task UpdateEvent(Guid id, EventCreateDto dto, string userId, bool isAdmin)
    {
        var e = await eventRepository.GetById(id);
        if (e == null) throw new Data.DAL.Exceptions.NotFoundException(EventNotFoundMessage);

        if (e.CreatorId != userId && !isAdmin)
            throw new UnauthorizedAccessException("Only the creator or an admin can update this event.");

        ApplyEventChanges(e, dto);
        await ApplyRequiredRolesAsync(e, dto.RequiredRoles);

        await eventRepository.Update(e);
        await eventNotifier.NotifyEventChangedAsync();
    }

    public async Task UpdateEventImage(Guid id, string userId, bool isAdmin, Stream imageContent, string contentType)
    {
        if (imageContent == null) throw new ArgumentNullException(nameof(imageContent));

        var e = await eventRepository.GetById(id);
        if (e == null) throw new Data.DAL.Exceptions.NotFoundException(EventNotFoundMessage);

        if (e.CreatorId != userId && !isAdmin)
            throw new UnauthorizedAccessException("Only the creator or an admin can update this event image.");

        var normalizedContentType = ContentTypeToExtensionMapper.NormalizeContentType(contentType)
            ?? throw new ArgumentException("Unsupported image content type. Only JPEG, PNG, WebP, HEIC, and HEIF are allowed.");

        var extension = ContentTypeToExtensionMapper.GetExtension(normalizedContentType);
        var name = $"{e.Title}-{Guid.NewGuid()}.{extension}";

        var blobItem = await blobStorageAgent.UploadFileToBlobAsync(name, normalizedContentType, imageContent);

        if (e.Image != null)
        {
            await blobStorageAgent.DeleteFileToBlobAsync(e.Image.FileName);
        }

        var eventImage = BuildEventImage(null, blobItem);
        e.Image = eventImage;

        await eventRepository.UpdateImage(id, eventImage);
        await eventNotifier.NotifyEventChangedAsync();
    }

    private static void ApplyEventChanges(Event e, EventCreateDto dto)
    {
        var sanitizer = new HtmlSanitizer();

        e.Title = dto.Title;
        e.Description = sanitizer.Sanitize(dto.Description);
        e.StartDateTime = dto.StartDateTime;
        e.EndDateTime = dto.EndDateTime;
        e.LocationName = dto.LocationName;
        e.Latitude = dto.Latitude;
        e.Longitude = dto.Longitude;
    }

    private static EventImage BuildEventImage(Guid? currentImageId, BlobStorageItem blobItem)
    {
        return new EventImage
        {
            Id = currentImageId ?? Guid.NewGuid(),
            FileName = blobItem.FileName,
            Uri = blobItem.Uri,
            ContentType = blobItem.ContentType,
            CreatedOn = blobItem.CreatedOn == default ? DateTime.UtcNow : blobItem.CreatedOn
        };
    }

    public async Task DeleteEvent(Guid id, string userId, bool isAdmin)
    {
        var e = await eventRepository.GetById(id);
        if (e == null) throw new Data.DAL.Exceptions.NotFoundException(EventNotFoundMessage);

        EnsureCanManageEvent(e, userId, isAdmin, "delete");

        await eventRepository.Delete(id);
        await eventNotifier.NotifyEventChangedAsync();
    }

    public async Task RespondToEvent(Guid id, string userId, EventResponseStatus status)
    {
        var e = await eventRepository.GetById(id);
        if (e == null) throw new Data.DAL.Exceptions.NotFoundException(EventNotFoundMessage);

        await EnsureUserCanRespondAsync(e, userId);

        var response = await eventRepository.GetResponse(id, userId);
        if (response == null)
        {
            await eventRepository.AddResponse(new EventResponse
            {
                EventId = id,
                UserId = userId,
                Status = status
            });
            return;
        }

        response.Status = status;
        await eventRepository.UpdateResponse(response);
    }

    private async Task<(IList<string>? userRoles, bool isAdmin)> GetUserAccessAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return (null, false);

        var userRoles = await applicationUserService.GetUserRoles(userId);
        return (userRoles, userRoles.Contains(Roles.Admin));
    }

    private static Event BuildEvent(EventCreateDto dto, string creatorId)
    {
        var sanitizer = new HtmlSanitizer();

        return new Event
        {
            Title = dto.Title,
            Description = sanitizer.Sanitize(dto.Description),
            StartDateTime = dto.StartDateTime,
            EndDateTime = dto.EndDateTime,
            CreatorId = creatorId,
            LocationName = dto.LocationName,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };
    }

    private async Task ApplyRequiredRolesAsync(Event e, IEnumerable<string>? requiredRoles)
    {
        e.RequiredRoles.Clear();
        if (requiredRoles == null || !requiredRoles.Any())
            return;

        await using var context = await dbContextFactory.CreateDbContextAsync();
        var rolesToFetch = requiredRoles.Except(Roles.AllProtected, StringComparer.OrdinalIgnoreCase);
        var newRoles = await context.Roles.Where(r => rolesToFetch.Contains(r.Name)).ToListAsync();
        foreach (var role in newRoles)
        {
            e.RequiredRoles.Add(role);
        }
    }

    private async Task ApplyImageAsync(Event e, EventCreateDto dto)
    {
        if (dto.ImageContent == null || dto.ImageContentType == null)
            return;

        var contentType = ContentTypeToExtensionMapper.NormalizeContentType(dto.ImageContentType.MediaType)
            ?? throw new ArgumentException("Unsupported image content type. Only JPEG, PNG, WebP, HEIC, and HEIF are allowed.");

        var extension = ContentTypeToExtensionMapper.GetExtension(contentType);
        var name = $"{dto.Title}-{Guid.NewGuid()}.{extension}";

        var blobItem = await blobStorageAgent.UploadFileToBlobAsync(name,
            contentType,
            dto.ImageContent);

        if (e.Image == null)
        {
            e.Image = EventImage.ToEventImage(blobItem);
            return;
        }

        var existingImage = e.Image;
        existingImage.FileName = blobItem.FileName;
        existingImage.Uri = blobItem.Uri;
        existingImage.ContentType = blobItem.ContentType;
        existingImage.CreatedOn = blobItem.CreatedOn == default ? DateTime.UtcNow : blobItem.CreatedOn;
    }

    private static void EnsureCanManageEvent(Event e, string userId, bool isAdmin, string action)
    {
        if (e.CreatorId != userId && !isAdmin)
            throw new UnauthorizedAccessException($"Only the creator or an admin can {action} this event.");
    }

    private async Task EnsureUserCanRespondAsync(Event e, string userId)
    {
        if (!e.RequiredRoles.Any())
            return;

        var userRoles = await applicationUserService.GetUserRoles(userId);
        if (!userRoles.Contains(Roles.Admin) && !e.RequiredRoles.Any(r => userRoles.Contains(r.Name!)))
        {
            throw new UnauthorizedAccessException("You don't have the required role to RSVP to this event.");
        }
    }

    private static EventDto MapToDto(Event e, string? userId)
    {
        var dto = new EventDto
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            ImageUrl = e.Image != null ? $"/api/v1/blob/event/image/{e.Image.Id}" : null,
            StartDateTime = e.StartDateTime,
            EndDateTime = e.EndDateTime,
            CreatorName = e.Creator?.FullName ?? "Unknown",
            CreatorProfilePictureUrl = e.Creator?.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{e.Creator.ProfileImageId}" : null,
            CreatorId = e.CreatorId,
            CreatedOn = e.CreatedOn,
            LocationName = e.LocationName,
            Latitude = e.Latitude,
            Longitude = e.Longitude,
            AcceptedUsers = e.Responses
                .Where(r => r.Status == EventResponseStatus.Accepted)
                .Select(r => new EventResponseUserDto 
                { 
                    UserId = r.UserId, 
                    UserName = r.User?.FullName ?? "Unknown",
                    ProfilePictureUrl = r.User?.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{r.User.ProfileImageId}" : null
                })
                .ToList(),
            DeclinedUsers = e.Responses
                .Where(r => r.Status == EventResponseStatus.Declined)
                .Select(r => new EventResponseUserDto 
                { 
                    UserId = r.UserId, 
                    UserName = r.User?.FullName ?? "Unknown",
                    ProfilePictureUrl = r.User?.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{r.User.ProfileImageId}" : null
                })
                .ToList(),
            RequiredRoles = e.RequiredRoles?.Select(r => r.Name ?? string.Empty).ToList() ?? new List<string>()
        };

        if (userId != null)
        {
            dto.UserResponse = e.Responses.FirstOrDefault(r => r.UserId == userId)?.Status;
        }

        return dto;
    }
}
