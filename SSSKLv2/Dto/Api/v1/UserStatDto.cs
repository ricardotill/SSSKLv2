namespace SSSKLv2.Dto.Api.v1;

public class UserStatDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
    public int TotalOrders { get; set; }
    public int TotalItemsBought { get; set; }
    public decimal TotalTopUp { get; set; }
    public int QuoteCount { get; set; }
    public int QuoteVotesGiven { get; set; }
    public int ReactionCount { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public int CurrentStreak { get; set; }
    public DateTime? MembershipStartDate { get; set; }
    public int MaxOrdersPerHour { get; set; }
    public int MinMinutesBetweenOrders { get; set; }
    public int MinMinutesBetweenTopUp { get; set; }
    public decimal MaxSingleTopUp { get; set; }
    public int QuoteVotesReceived { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public DateTime? LastTopUpDate { get; set; }
    public DateTime? LastStatsRecalculatedAt { get; set; }
}