using MediatR;

namespace SSSKL.Application.Features.Blobs.Queries.GetSocialPreview;

/// <summary>An event image cropped to 1.91:1 and sized 300-1200px wide as JPEG, for Open Graph previews.</summary>
public sealed record GetSocialPreviewQuery(Guid Id) : IRequest<SocialPreview>;

/// <param name="CreatedOn">When the source image was stored, for Last-Modified.</param>
public sealed record SocialPreview(Stream Content, DateTime CreatedOn);
