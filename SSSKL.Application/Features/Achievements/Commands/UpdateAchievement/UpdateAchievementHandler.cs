using MediatR;
using SSSKL.Application.Util;
using SSSKL.Contracts.Agents;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Achievements.Commands.UpdateAchievement;

internal sealed class UpdateAchievementHandler(
    IAchievementRepository achievementRepository,
    IBlobStorageAgent blobStorageAgent) : IRequestHandler<UpdateAchievementCommand>
{
    public async Task Handle(UpdateAchievementCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Achievement;
        var achievement = new Achievement
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            AutoAchieve = dto.AutoAchieve,
            Action = dto.Action,
            ComparisonOperator = dto.ComparisonOperator,
            ComparisonValue = dto.ComparisonValue,
            Tier = Enum.Parse<Achievement.AchievementTier>(dto.Tier ?? "Bronze"),
            ParentAchievementId = dto.ParentAchievementId
        };

        if (request.NewImage != null)
        {
            await ReplaceImage(achievement, request.NewImage, request.NewImageContentType);
            return;
        }

        if (dto.Image != null)
        {
            achievement.Image = new AchievementImage
            {
                Id = dto.Image.Id,
                FileName = dto.Image.FileName,
                Uri = dto.Image.Uri,
                ContentType = dto.Image.ContentType,
                CreatedOn = DateTime.Now
            };
        }
        else
        {
            // The edit form never round-trips the current image, so without this the
            // repository would treat a missing image field as "remove the image".
            var existing = await achievementRepository.GetById(dto.Id);
            achievement.Image = existing.Image;
        }

        await achievementRepository.Update(achievement);
    }

    private async Task ReplaceImage(Achievement achievement, Stream newImageContent, string? newImageContentType)
    {
        var contentType = ContentTypeToExtensionMapper.NormalizeContentType(newImageContentType)
            ?? throw new ArgumentException("Unsupported image content type. Only JPEG, PNG, WebP, HEIC, and HEIF are allowed.");

        // Throws NotFoundException before uploading if the achievement no longer exists.
        var existing = await achievementRepository.GetById(achievement.Id);

        var extension = ContentTypeToExtensionMapper.GetExtension(contentType);
        var name = $"{achievement.Name}-{Guid.NewGuid()}.{extension}";
        var blobItem = await blobStorageAgent.UploadFileToBlobAsync(name, contentType, newImageContent);

        achievement.Image = AchievementImage.ToAchievementImage(blobItem);
        await achievementRepository.Update(achievement);

        if (existing.Image != null)
        {
            await blobStorageAgent.DeleteFileToBlobAsync(existing.Image.FileName);
        }
    }
}
