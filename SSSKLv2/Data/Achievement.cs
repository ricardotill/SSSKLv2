namespace SSSKLv2.Data;

public class Achievement : BaseModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public BlobStorageItem Image { get; set; }
    public UserAction Action { get; set; }
    public ComparisonOperator Comparison { get; set; }
    public decimal ComparisonValue { get; set; }
    public IEnumerable<AchievementEntry> CompletedEntries { get; set; }
    
    public enum UserAction
    {
        Buy, TopUp
    }
    public enum ComparisonOperator
    {
        LessThan, GreaterThan,
        LessThanOrEqual, GreaterThanOrEqual
    }
}