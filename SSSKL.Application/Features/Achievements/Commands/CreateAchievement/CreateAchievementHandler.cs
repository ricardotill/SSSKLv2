using MediatR;
using SSSKL.Application.Util;
using SSSKL.Contracts.Agents;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Achievements.Commands.CreateAchievement;

internal sealed class CreateAchievementHandler(
    IAchievementRepository achievementRepository,
    IBlobStorageAgent blobStorageAgent) : IRequestHandler<CreateAchievementCommand>
{
    public async Task Handle(CreateAchievementCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Achievement;
        if (dto.ImageContentType == null || dto.ImageContent == null)
        {
            throw new ArgumentException("Image content and type are required.");
        }

        var contentType = ContentTypeToExtensionMapper.NormalizeContentType(dto.ImageContentType.MediaType)
            ?? throw new ArgumentException("Unsupported image content type. Only JPEG, PNG, WebP, HEIC, and HEIF are allowed.");

        var extension = ContentTypeToExtensionMapper.GetExtension(contentType);
        var name = $"{dto.Name}-{Guid.NewGuid()}.{extension}";

        var blobItem = await blobStorageAgent.UploadFileToBlobAsync(name, contentType, dto.ImageContent);

        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Image = AchievementImage.ToAchievementImage(blobItem),
            AutoAchieve = dto.AutoAchieve,
            Action = dto.Action,
            ComparisonOperator = dto.ComparisonOperator,
            ComparisonValue = dto.ComparisonValue,
            Tier = Enum.Parse<Achievement.AchievementTier>(dto.Tier ?? "Bronze"),
            ParentAchievementId = dto.ParentAchievementId
        };
        await achievementRepository.Create(achievement);
    }
}
