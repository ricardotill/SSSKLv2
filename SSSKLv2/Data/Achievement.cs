namespace SSSKLv2.Data;

public class Achievement : BaseModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool AutoAchieve { get; set; }
    public AchievementImage? Image { get; set; }
    public ActionOption Action { get; set; }
    public ComparisonOperatorOption ComparisonOperator { get; set; }
    public int ComparisonValue { get; set; }
    public IEnumerable<AchievementEntry> CompletedEntries { get; set; }
    
    public enum ActionOption
    {
        None = 0,
        UserBuy = 1,
        TotalBuy = 2, 
        UserTopUp = 3,
        TotalTopUp = 4,
        YearsOfMembership = 5
    }
    public enum ComparisonOperatorOption
    {
        None = 0,
        LessThan = 1,
        GreaterThan = 2,
        LessThanOrEqual = 3, 
        GreaterThanOrEqual = 4
    }
}