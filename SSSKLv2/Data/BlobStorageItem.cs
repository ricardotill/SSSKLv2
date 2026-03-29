namespace SSSKLv2.Data;

public class BlobStorageItem : BaseModel
{
    public string FileName { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}