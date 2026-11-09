using MediatR;

namespace SSSKL.Application.Features.Achievements.Commands.AwardAchievement;

/// <summary>Returns false when the user already has the achievement or it doesn't exist.</summary>
public sealed record AwardAchievementToUserCommand(string UserId, Guid AchievementId) : IRequest<bool>;
