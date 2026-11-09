using MediatR;
using SSSKL.Contracts.Dto;

namespace SSSKL.Application.Features.Achievements.Queries.GetAchievementListing;

/// <summary>Every achievement with the user's progress. Identify the user by id or by username.</summary>
public sealed record GetAchievementListingQuery(string? UserId = null, string? Username = null) : IRequest<IList<AchievementListingDto>>;
