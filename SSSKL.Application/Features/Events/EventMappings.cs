using SSSKL.Contracts.Dto.Api;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Events;

public static class EventMappings
{
    /// <param name="userId">The viewing user; fills <see cref="EventDto.UserResponse"/> when set.</param>
    public static EventDto ToDto(this Event e, string? userId)
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
            AcceptedUsers = MapResponses(e, EventResponseStatus.Accepted),
            DeclinedUsers = MapResponses(e, EventResponseStatus.Declined),
            RequiredRoles = e.RequiredRoles?.Select(r => r.Name ?? string.Empty).ToList() ?? new List<string>()
        };

        if (userId != null)
        {
            dto.UserResponse = e.Responses.FirstOrDefault(r => r.UserId == userId)?.Status;
        }

        return dto;
    }

    private static List<EventResponseUserDto> MapResponses(Event e, EventResponseStatus status) =>
        e.Responses
            .Where(r => r.Status == status)
            .Select(r => new EventResponseUserDto
            {
                UserId = r.UserId,
                UserName = r.User?.FullName ?? "Unknown",
                ProfilePictureUrl = r.User?.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{r.User.ProfileImageId}" : null
            })
            .ToList();
}
