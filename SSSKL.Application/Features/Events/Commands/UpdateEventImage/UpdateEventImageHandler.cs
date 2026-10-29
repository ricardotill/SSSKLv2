using MediatR;
using SSSKL.Application.Util;
using SSSKL.Contracts.Agents;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Events.Commands.UpdateEventImage;

internal sealed class UpdateEventImageHandler(
    IEventRepository eventRepository,
    IBlobStorageAgent blobStorageAgent,
    IEventNotifier eventNotifier) : IRequestHandler<UpdateEventImageCommand>
{
    public async Task Handle(UpdateEventImageCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.ImageContent);

        var e = await eventRepository.GetExisting(request.Id);
        EventAccess.EnsureCanManage(e, request.UserId, request.IsAdmin, "update the image of");

        var contentType = ContentTypeToExtensionMapper.NormalizeContentType(request.ContentType)
            ?? throw new ArgumentException("Unsupported image content type. Only JPEG, PNG, WebP, HEIC, and HEIF are allowed.");

        var extension = ContentTypeToExtensionMapper.GetExtension(contentType);
        var name = $"{e.Title}-{Guid.NewGuid()}.{extension}";

        var blobItem = await blobStorageAgent.UploadFileToBlobAsync(name, contentType, request.ImageContent);

        if (e.Image != null)
        {
            await blobStorageAgent.DeleteFileToBlobAsync(e.Image.FileName);
        }

        var eventImage = new EventImage
        {
            Id = Guid.NewGuid(),
            FileName = blobItem.FileName,
            Uri = blobItem.Uri,
            ContentType = blobItem.ContentType,
            CreatedOn = blobItem.CreatedOn == default ? DateTime.UtcNow : blobItem.CreatedOn
        };
        e.Image = eventImage;

        await eventRepository.UpdateImage(request.Id, eventImage);
        await eventNotifier.NotifyEventChangedAsync();
    }
}
