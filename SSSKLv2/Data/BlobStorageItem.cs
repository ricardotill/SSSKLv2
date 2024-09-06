namespace SSSKLv2.Data;

public class BlobStorageItem : BaseModel
{
    public string FileName { get; set; }
    public string Uri { get; set; }
    public string ContentType { get; set; }
}

public class AchievementImage : BlobStorageItem
{
    public Achievement Achievement { get; set; }
}