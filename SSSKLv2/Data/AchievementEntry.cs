namespace SSSKLv2.Data;

public class AchievementEntry : BaseModel
{
    public Achievement Achievement { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public Achievement.AchievementTier Tier { get; set; }
    public bool HasSeen { get; set; }
}