namespace SSSKLv2.Data;

public class UserImage : BlobStorageItem
{
    public ApplicationUser User { get; set; } = default!;

    public static UserImage ToUserImage(BlobStorageItem item) => new()
    {
        Id = item.Id,
        FileName = item.FileName,
        Uri = item.Uri,
        ContentType = item.ContentType,
        CreatedOn = item.CreatedOn
    };
}
