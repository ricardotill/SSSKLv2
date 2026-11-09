using MediatR;

namespace SSSKL.Application.Features.Achievements.Commands.DeleteAchievement;

public sealed record DeleteAchievementCommand(Guid Id) : IRequest;
