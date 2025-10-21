namespace SSSKLv2.Data;

public class AchievementImage : BlobStorageItem
{
    public Achievement Achievement { get; set; }
    
    public static AchievementImage ToAchievementImage(BlobStorageItem item) => new()
    {
        Id = item.Id,
        FileName = item.FileName,
        Uri = item.Uri,
        ContentType = item.ContentType,
        CreatedOn = item.CreatedOn
    };
}