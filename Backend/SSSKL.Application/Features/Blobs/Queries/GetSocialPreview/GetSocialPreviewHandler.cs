using MediatR;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SSSKL.Application.Util;
using SSSKL.Contracts.Agents;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.Blobs.Queries.GetSocialPreview;

internal sealed class GetSocialPreviewHandler(IBlobStorageItemRepository blobStorageItemRepository, IBlobStorageAgent blobAgent)
    : IRequestHandler<GetSocialPreviewQuery, SocialPreview>
{
    // Industry standard aspect ratio for Open Graph images
    private const double TargetRatio = 1.91;

    public async Task<SocialPreview> Handle(GetSocialPreviewQuery request, CancellationToken cancellationToken)
    {
        var image = await blobStorageItemRepository.GetById(request.Id)
                    ?? throw new NotFoundException("Image not found");

        using var stream = await blobAgent.OpenDownloadStreamAsync(image.FileName);
        using var sourceImage = await SafeImageLoader.LoadAsync(stream, cancellationToken);

        var currentRatio = (double)sourceImage.Width / sourceImage.Height;
        if (Math.Abs(currentRatio - TargetRatio) > 0.05)
        {
            // Center crop to the target ratio
            var newWidth = sourceImage.Width;
            var newHeight = sourceImage.Height;

            if (currentRatio > TargetRatio)
            {
                newWidth = Math.Max(1, (int)(sourceImage.Height * TargetRatio));
            }
            else
            {
                newHeight = Math.Max(1, (int)(sourceImage.Width / TargetRatio));
            }

            sourceImage.Mutate(x => x.Crop(new Rectangle(
                (sourceImage.Width - newWidth) / 2,
                (sourceImage.Height - newHeight) / 2,
                newWidth,
                newHeight)));
        }

        // Minimum 300px for WhatsApp, maximum 1200px for performance
        if (sourceImage.Width > 1200)
        {
            sourceImage.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(1200, 0),
                Mode = ResizeMode.Max
            }));
        }
        else if (sourceImage.Width < 300)
        {
            sourceImage.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(300, 0),
                Mode = ResizeMode.BoxPad // Upscale safely
            }));
        }

        var outputStream = new MemoryStream();
        // WhatsApp is sensitive to file size (ideally < 300KB); quality 75 balances size and looks
        await sourceImage.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 75 }, cancellationToken);
        outputStream.Position = 0;

        return new SocialPreview(outputStream, image.CreatedOn);
    }
}
