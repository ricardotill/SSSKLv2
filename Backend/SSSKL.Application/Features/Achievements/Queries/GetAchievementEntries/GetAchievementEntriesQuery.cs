using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Achievements.Queries.GetAchievementEntries;

/// <summary>The achievements a user has earned. Identify the user by id or by username.</summary>
public sealed record GetAchievementEntriesQuery(string? UserId = null, string? Username = null) : IRequest<IReadOnlyList<AchievementEntryDto>>;
