namespace SSSKLv2.Data;

public class EventImage : BlobStorageItem
{
    public Event Event { get; set; } = default!;

    public static EventImage ToEventImage(BlobStorageItem item) => new()
    {
        Id = item.Id,
        FileName = item.FileName,
        Uri = item.Uri,
        ContentType = item.ContentType,
        CreatedOn = item.CreatedOn
    };
}
