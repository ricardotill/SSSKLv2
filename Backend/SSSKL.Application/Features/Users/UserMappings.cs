using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Users;

public static class UserMappings
{
    public static ApplicationUserDto ToDto(this ApplicationUser u) => new()
    {
        Id = u.Id,
        UserName = u.UserName ?? string.Empty,
        FullName = u.FullName,
        Saldo = u.Saldo,
        LastOrdered = u.LastOrdered,
        ProfilePictureUrl = u.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{u.ProfileImageId}" : null,
        Description = u.Description
    };

    public static UserStatDto ToDto(this UserStat stats) => new()
    {
        Id = stats.Id,
        UserId = stats.UserId,
        TotalSpent = stats.TotalSpent,
        TotalOrders = stats.TotalOrders,
        TotalItemsBought = stats.TotalItemsBought,
        TotalTopUp = stats.TotalTopUp,
        QuoteCount = stats.QuoteCount,
        QuoteVotesGiven = stats.QuoteVotesGiven,
        ReactionCount = stats.ReactionCount,
        LastActivityDate = stats.LastActivityDate,
        CurrentStreak = stats.CurrentStreak,
        MembershipStartDate = stats.MembershipStartDate,
        MaxOrdersPerHour = stats.MaxOrdersPerHour,
        MinMinutesBetweenOrders = stats.MinMinutesBetweenOrders,
        MinMinutesBetweenTopUp = stats.MinMinutesBetweenTopUp,
        MaxSingleTopUp = stats.MaxSingleTopUp,
        QuoteVotesReceived = stats.QuoteVotesReceived,
        LastOrderDate = stats.LastOrderDate,
        LastTopUpDate = stats.LastTopUpDate,
        LastStatsRecalculatedAt = stats.LastStatsRecalculatedAt
    };

    /// <summary>The detailed view for admins and the user themselves. Never includes the password.</summary>
    public static ApplicationUserDetailedDto ToDetailedDto(this ApplicationUser u, IList<string> roles) => new()
    {
        Id = u.Id,
        UserName = u.UserName ?? string.Empty,
        Email = u.Email,
        EmailConfirmed = u.EmailConfirmed,
        PhoneNumber = u.PhoneNumber,
        PhoneNumberConfirmed = u.PhoneNumberConfirmed,
        Name = u.Name,
        Surname = u.Surname,
        FullName = u.FullName,
        Saldo = u.Saldo,
        LastOrdered = u.LastOrdered,
        ProfilePictureUrl = u.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{u.ProfileImageId}" : null,
        Description = u.Description,
        Roles = roles
    };
}
