using MediatR;

namespace SSSKL.Application.Features.Blobs.Queries.GetBlobImage;

/// <summary>Opens a stored image (event, achievement or profile picture) for download.</summary>
public sealed record GetBlobImageQuery(Guid Id) : IRequest<BlobImage>;

public sealed record BlobImage(Stream Content, string ContentType);
