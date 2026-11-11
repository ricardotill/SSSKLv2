using MediatR;
using SSSKL.Application.Util;
using SSSKL.Contracts.Agents;
using SSSKL.Contracts.Dto.Api;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Events.Commands.CreateEvent;

internal sealed class CreateEventHandler(
    IEventRepository eventRepository,
    IRoleRepository roleRepository,
    IBlobStorageAgent blobStorageAgent,
    IEventNotifier eventNotifier) : IRequestHandler<CreateEventCommand, EventDto>
{
    public async Task<EventDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Event;
        var e = new Event { CreatorId = request.CreatorId };
        EventAccess.ApplyDetails(e, dto);
        await EventAccess.ApplyRequiredRoles(roleRepository, e, dto.RequiredRoles);
        await ApplyImage(e, dto);

        await eventRepository.Add(e);
        await eventNotifier.NotifyEventChangedAsync();

        var created = await eventRepository.GetExisting(e.Id);
        return created.ToDto(request.CreatorId);
    }

    private async Task ApplyImage(Event e, EventCreateDto dto)
    {
        if (dto.ImageContent == null || dto.ImageContentType == null)
            return;

        var contentType = ContentTypeToExtensionMapper.NormalizeContentType(dto.ImageContentType.MediaType)
            ?? throw new ArgumentException("Unsupported image content type. Only JPEG, PNG, WebP, HEIC, and HEIF are allowed.");

        var extension = ContentTypeToExtensionMapper.GetExtension(contentType);
        var name = $"{dto.Title}-{Guid.NewGuid()}.{extension}";

        var blobItem = await blobStorageAgent.UploadFileToBlobAsync(name, contentType, dto.ImageContent);
        e.Image = EventImage.ToEventImage(blobItem);
    }
}
