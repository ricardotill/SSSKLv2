using MediatR;
using SSSKL.Contracts.Dto;

namespace SSSKL.Application.Features.Leaderboards.Queries.GetLeaderboard;

public enum LeaderboardPeriod
{
    /// <summary>All-time totals.</summary>
    All,
    /// <summary>The current calendar month.</summary>
    Monthly,
    /// <summary>The last 12 hours.</summary>
    TwelveHours,
    /// <summary>The last 12 hours, limited to the ten most recently active users.</summary>
    TwelveHoursLive
}

/// <summary>The top 18 users for a product, cached for 30 seconds.</summary>
public sealed record GetLeaderboardQuery(Guid ProductId, LeaderboardPeriod Period) : IRequest<IReadOnlyList<LeaderboardEntryDto>>;
