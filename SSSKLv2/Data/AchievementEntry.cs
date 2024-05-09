namespace SSSKLv2.Data;

public class AchievementEntry : BaseModel
{
    public Achievement Achievement { get; set; }
    public ApplicationUser User { get; set; }
    public bool HasSeen { get; set; }
}