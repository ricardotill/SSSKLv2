using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSKLv2.Data;

public class UserStat : BaseModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSpent { get; set; }
    public int TotalOrders { get; set; }
    public int TotalItemsBought { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalTopUp { get; set; }
    public int QuoteCount { get; set; }
    public int QuoteVotesGiven { get; set; }
    public int ReactionCount { get; set; }
    
    public DateTime? LastActivityDate { get; set; }
    public int CurrentStreak { get; set; }

    // New fields for extended achievements
    public DateTime? MembershipStartDate { get; set; }
    public int MaxOrdersPerHour { get; set; }
    public int MinMinutesBetweenOrders { get; set; } = int.MaxValue;
    public int MinMinutesBetweenTopUp { get; set; } = int.MaxValue;
    [Column(TypeName = "decimal(18,2)")]
    public decimal MaxSingleTopUp { get; set; }
    public int QuoteVotesReceived { get; set; }
    
    // Tracking for window-based/interval stats
    public DateTime? LastOrderDate { get; set; }
    public DateTime? LastTopUpDate { get; set; }
}
