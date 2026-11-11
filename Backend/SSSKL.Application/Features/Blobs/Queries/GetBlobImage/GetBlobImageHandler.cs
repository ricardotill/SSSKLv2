using MediatR;
using SSSKL.Application.Util;
using SSSKL.Contracts.Agents;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.Blobs.Queries.GetBlobImage;

internal sealed class GetBlobImageHandler(IBlobStorageItemRepository blobStorageItemRepository, IBlobStorageAgent blobAgent)
    : IRequestHandler<GetBlobImageQuery, BlobImage>
{
    public async Task<BlobImage> Handle(GetBlobImageQuery request, CancellationToken cancellationToken)
    {
        var image = await blobStorageItemRepository.GetById(request.Id)
                    ?? throw new NotFoundException("Image not found");

        var contentType = ContentTypeToExtensionMapper.NormalizeContentType(image.ContentType)
                          ?? throw new ArgumentException("Unsupported image content type.");

        var stream = await blobAgent.OpenDownloadStreamAsync(image.FileName);
        return new BlobImage(stream, contentType);
    }
}
