using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Users.Stats;

/// <summary>Recalculates one user's statistics. Non-admins may do this for themselves once every seven days.</summary>
public sealed record RecalculateUserStatsCommand(string UserId, string? RequesterId, bool IsAdmin) : IRequest<RecalculateUserStatsResult>;

/// <param name="Stats">The recalculated stats, or null when the user hit the seven-day limit.</param>
/// <param name="NextAllowedAt">When the limit lifts; set only when <paramref name="Stats"/> is null.</param>
public sealed record RecalculateUserStatsResult(UserStatDto? Stats, DateTime? NextAllowedAt);
