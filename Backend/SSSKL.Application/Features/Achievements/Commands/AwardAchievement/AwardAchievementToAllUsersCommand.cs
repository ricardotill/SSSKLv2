using MediatR;

namespace SSSKL.Application.Features.Achievements.Commands.AwardAchievement;

/// <summary>Returns the number of users that received the achievement.</summary>
public sealed record AwardAchievementToAllUsersCommand(Guid AchievementId) : IRequest<int>;
