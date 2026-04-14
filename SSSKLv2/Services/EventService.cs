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

public class EventService(IEventRepository eventRepository, IBlobStorageAgent blobStorageAgent, IApplicationUserService applicationUserService, ApplicationDbContext dbContext, IEventNotifier eventNotifier) : IEventService
{
    public async Task<IEnumerable<EventDto>> GetAllEvents(int skip = 0, int take = 15, bool futureOnly = false, string? userId = null, string? requiredRole = null)
    {
        IList<string>? userRoles = null;
        bool isAdmin = false;
        if (userId != null)
        {
            userRoles = await applicationUserService.GetUserRoles(userId);
            isAdmin = userRoles.Contains(Roles.Admin);
        }
        
        var events = await eventRepository.GetAll(skip, take, futureOnly, userRoles, isAdmin, requiredRole);
        return events.Select(e => MapToDto(e, userId));
    }

    public async Task<int> GetCount(bool futureOnly = false, string? userId = null, string? requiredRole = null)
    {
        IList<string>? userRoles = null;
        bool isAdmin = false;
        if (userId != null)
        {
            userRoles = await applicationUserService.GetUserRoles(userId);
            isAdmin = userRoles.Contains(Roles.Admin);
        }
        
        return await eventRepository.GetCount(futureOnly, userRoles, isAdmin, requiredRole);
    }

    public async Task<EventDto> GetEventById(Guid id, string? userId = null)
    {
        var e = await eventRepository.GetById(id);
        if (e == null) throw new Data.DAL.Exceptions.NotFoundException("Event not found");
        return MapToDto(e, userId);
    }

    public async Task<Guid> CreateEvent(EventCreateDto dto, string creatorId)
    {
        var sanitizer = new HtmlSanitizer();

        var e = new Event
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

        if (dto.RequiredRoles != null && dto.RequiredRoles.Any())
        {
            var rolesToFetch = dto.RequiredRoles.Except(Roles.AllProtected, StringComparer.OrdinalIgnoreCase);
            e.RequiredRoles = await dbContext.Roles.Where(r => rolesToFetch.Contains(r.Name)).ToListAsync();
        }

        if (dto.ImageContent != null && dto.ImageContentType != null)
        {
            var extension = ContentTypeToExtensionMapper.GetExtension(dto.ImageContentType.MediaType);
            var name = $"{dto.Title}-{Guid.NewGuid()}.{extension}";
            
            var blobItem = await blobStorageAgent.UploadFileToBlobAsync(name,
                dto.ImageContentType.MediaType,
                dto.ImageContent);
            
            e.Image = EventImage.ToEventImage(blobItem);
        }

        await eventRepository.Add(e);
        await eventNotifier.NotifyEventChangedAsync();
        return e.Id;
    }

    public async Task UpdateEvent(Guid id, EventCreateDto dto, string userId, bool isAdmin)
    {
        var e = await eventRepository.GetById(id);
        if (e == null) throw new Data.DAL.Exceptions.NotFoundException("Event not found");

        if (e.CreatorId != userId && !isAdmin)
            throw new UnauthorizedAccessException("Only the creator or an admin can update this event.");

        var sanitizer = new HtmlSanitizer();

        e.Title = dto.Title;
        e.Description = sanitizer.Sanitize(dto.Description);
        e.StartDateTime = dto.StartDateTime;
        e.EndDateTime = dto.EndDateTime;
        e.LocationName = dto.LocationName;
        e.Latitude = dto.Latitude;
        e.Longitude = dto.Longitude;

        e.RequiredRoles.Clear();
        if (dto.RequiredRoles != null && dto.RequiredRoles.Any())
        {
            var rolesToFetch = dto.RequiredRoles.Except(Roles.AllProtected, StringComparer.OrdinalIgnoreCase);
            var newRoles = await dbContext.Roles.Where(r => rolesToFetch.Contains(r.Name)).ToListAsync();
            foreach (var r in newRoles)
            {
                e.RequiredRoles.Add(r);
            }
        }

        if (dto.ImageContent != null && dto.ImageContentType != null)
        {
            var extension = ContentTypeToExtensionMapper.GetExtension(dto.ImageContentType.MediaType);
            var name = $"{dto.Title}-{Guid.NewGuid()}.{extension}";
            
            var blobItem = await blobStorageAgent.UploadFileToBlobAsync(name,
                dto.ImageContentType.MediaType,
                dto.ImageContent);
            
            e.Image = EventImage.ToEventImage(blobItem);
        }

        await eventRepository.Update(e);
        await eventNotifier.NotifyEventChangedAsync();
    }

    public async Task DeleteEvent(Guid id, string userId, bool isAdmin)
    {
        var e = await eventRepository.GetById(id);
        if (e == null) throw new Data.DAL.Exceptions.NotFoundException("Event not found");

        if (e.CreatorId != userId && !isAdmin)
            throw new UnauthorizedAccessException("Only the creator or an admin can delete this event.");

        await eventRepository.Delete(id);
        await eventNotifier.NotifyEventChangedAsync();
    }

    public async Task RespondToEvent(Guid id, string userId, EventResponseStatus status)
    {
        var e = await eventRepository.GetById(id);
        if (e == null) throw new Data.DAL.Exceptions.NotFoundException("Event not found");

        if (e.RequiredRoles.Any())
        {
            var userRoles = await applicationUserService.GetUserRoles(userId);
            if (!userRoles.Contains(Roles.Admin) && !e.RequiredRoles.Any(r => userRoles.Contains(r.Name!)))
            {
                throw new UnauthorizedAccessException("You don't have the required role to RSVP to this event.");
            }
        }

        var response = await eventRepository.GetResponse(id, userId);
        if (response == null)
        {
            await eventRepository.AddResponse(new EventResponse
            {
                EventId = id,
                UserId = userId,
                Status = status
            });
        }
        else
        {
            response.Status = status;
            await eventRepository.UpdateResponse(response);
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
