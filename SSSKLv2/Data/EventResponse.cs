using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SSSKLv2.Data;

public class EventResponse : BaseModel
{
    [Required]
    public Guid EventId { get; set; }

    [ForeignKey("EventId")]
    public Event Event { get; set; } = default!;

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = default!;

    [Required]
    public EventResponseStatus Status { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter<EventResponseStatus>))]
public enum EventResponseStatus
{
    Accepted = 0,
    Declined = 1
}
