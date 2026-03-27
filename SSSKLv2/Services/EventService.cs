using SSSKLv2.Agents;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Dto.Api;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Util;

namespace SSSKLv2.Services;

public class EventService(IEventRepository eventRepository, IBlobStorageAgent blobStorageAgent, ILogger<EventService> logger) : IEventService
{
    public async Task<IEnumerable<EventDto>> GetAllEvents(int skip = 0, int take = 15, bool futureOnly = false, string? userId = null)
    {
        var events = await eventRepository.GetAll(skip, take, futureOnly);
        return events.Select(e => MapToDto(e, userId));
    }

    public async Task<int> GetCount(bool futureOnly = false)
    {
        return await eventRepository.GetCount(futureOnly);
    }

    public async Task<EventDto> GetEventById(Guid id, string? userId = null)
    {
        var e = await eventRepository.GetById(id);
        if (e == null) throw new Data.DAL.Exceptions.NotFoundException("Event not found");
        return MapToDto(e, userId);
    }

    public async Task<Guid> CreateEvent(EventCreateDto dto, string creatorId)
    {
        var e = new Event
        {
            Title = dto.Title,
            Description = dto.Description,
            StartDateTime = dto.StartDateTime,
            EndDateTime = dto.EndDateTime,
            CreatorId = creatorId
        };

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
        return e.Id;
    }

    public async Task UpdateEvent(Guid id, EventCreateDto dto, string userId, bool isAdmin)
    {
        var e = await eventRepository.GetById(id);
        if (e == null) throw new Data.DAL.Exceptions.NotFoundException("Event not found");

        if (e.CreatorId != userId && !isAdmin)
            throw new UnauthorizedAccessException("Only the creator or an admin can update this event.");

        e.Title = dto.Title;
        e.Description = dto.Description;
        e.StartDateTime = dto.StartDateTime;
        e.EndDateTime = dto.EndDateTime;

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
    }

    public async Task DeleteEvent(Guid id, string userId, bool isAdmin)
    {
        var e = await eventRepository.GetById(id);
        if (e == null) throw new Data.DAL.Exceptions.NotFoundException("Event not found");

        if (e.CreatorId != userId && !isAdmin)
            throw new UnauthorizedAccessException("Only the creator or an admin can delete this event.");

        await eventRepository.Delete(id);
    }

    public async Task RespondToEvent(Guid id, string userId, EventResponseStatus status)
    {
        var e = await eventRepository.GetById(id);
        if (e == null) throw new Data.DAL.Exceptions.NotFoundException("Event not found");

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
            CreatedOn = e.CreatedOn,
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
                .ToList()
        };

        if (userId != null)
        {
            dto.UserResponse = e.Responses.FirstOrDefault(r => r.UserId == userId)?.Status;
        }

        return dto;
    }
}
