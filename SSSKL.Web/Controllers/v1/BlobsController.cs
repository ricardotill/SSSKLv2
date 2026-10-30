using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKL.Application.Features.Blobs.Queries.GetBlobImage;
using SSSKL.Application.Features.Blobs.Queries.GetSocialPreview;

namespace SSSKL.Web.Controllers.v1;

[Authorize]
[Route("v1/blob")]
[ApiController]
public class BlobsController(ISender sender) : ControllerBase
{
    [HttpGet("event/image/{id}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 2592000, Location = ResponseCacheLocation.Any)]
    public Task<IActionResult> GetEventImage(Guid id) => GetImage(id);

    [HttpGet("achievement/image/{id}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 2592000, Location = ResponseCacheLocation.Any)]
    public Task<IActionResult> GetAchievementImage(Guid id) => GetImage(id);

    [HttpGet("profilepicture/image/{id}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 2592000, Location = ResponseCacheLocation.Any)]
    public Task<IActionResult> GetProfilePicture(Guid id) => GetImage(id);

    [HttpGet("event/image/{id}/social-preview.jpg")]
    [AllowAnonymous]
    [ResponseCache(Duration = 2592000, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetSocialPreview(Guid id)
    {
        var preview = await sender.Send(new GetSocialPreviewQuery(id));

        // Binary headers help social crawlers cache and verify the image
        if (Response != null)
        {
            Response.Headers.AcceptRanges = "bytes";
            Response.Headers.ETag = $"\"{id:N}\"";
            Response.Headers.LastModified = preview.CreatedOn.ToUniversalTime().ToString("R");
        }

        return File(preview.Content, "image/jpeg");
    }

    private async Task<IActionResult> GetImage(Guid id)
    {
        var image = await sender.Send(new GetBlobImageQuery(id));
        return File(image.Content, image.ContentType);
    }
}
