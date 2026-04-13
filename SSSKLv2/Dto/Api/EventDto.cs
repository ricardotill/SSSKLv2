using System.Net.Mime;
using SSSKLv2.Data;

namespace SSSKLv2.Dto.Api;

public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public string? CreatorProfilePictureUrl { get; set; }
    public DateTime CreatedOn { get; set; }
    public List<EventResponseUserDto> AcceptedUsers { get; set; } = new();
    public List<EventResponseUserDto> DeclinedUsers { get; set; } = new();
    public EventResponseStatus? UserResponse { get; set; }
    public string? LocationName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public IList<string> RequiredRoles { get; set; } = new List<string>();
}

public class EventCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Stream? ImageContent { get; set; }
    public ContentType? ImageContentType { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? LocationName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public IList<string> RequiredRoles { get; set; } = new List<string>();
}

public class EventResponseDto
{
    public EventResponseStatus Status { get; set; }
}

public class EventResponseUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}
