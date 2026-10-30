using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Users.Stats;

/// <summary>A user's statistics; only the user themselves or an admin may read them.</summary>
public sealed record GetUserStatsQuery(string UserId, string? RequesterId, bool IsAdmin) : IRequest<UserStatDto>;
