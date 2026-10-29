using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Achievements.Commands.UpdateAchievement;

/// <param name="NewImage">Decoded replacement image, or null to keep or set the image from <see cref="AchievementUpdateDto.Image"/>.</param>
public sealed record UpdateAchievementCommand(AchievementUpdateDto Achievement, Stream? NewImage = null, string? NewImageContentType = null) : IRequest;
