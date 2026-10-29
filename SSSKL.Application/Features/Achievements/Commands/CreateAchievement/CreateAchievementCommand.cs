using MediatR;
using SSSKL.Contracts.Dto;

namespace SSSKL.Application.Features.Achievements.Commands.CreateAchievement;

public sealed record CreateAchievementCommand(AchievementDto Achievement) : IRequest;
