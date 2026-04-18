using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Agents;
using SSSKLv2.Data;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace SSSKLv2.Controllers.v1;

[Route("v1/blob")]
[ApiController]
public class BlobsController : ControllerBase
{
    private readonly IBlobStorageAgent _blobAgent;
    private readonly ApplicationDbContext _context;

    public BlobsController(IBlobStorageAgent blobAgent, ApplicationDbContext context)
    {
        _blobAgent = blobAgent;
        _context = context;
    }

    [HttpGet("event/image/{id}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 2592000, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetEventImage(Guid id)
    {
        var image = await _context.BlobStorageItem.FirstOrDefaultAsync(x => x.Id == id);
        if (image == null) return NotFound();

        var stream = await _blobAgent.OpenDownloadStreamAsync(image.FileName);
        return File(stream, image.ContentType);
    }

    [HttpGet("achievement/image/{id}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 2592000, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetAchievementImage(Guid id)
    {
        var image = await _context.BlobStorageItem.FirstOrDefaultAsync(x => x.Id == id);
        if (image == null) return NotFound();

        var stream = await _blobAgent.OpenDownloadStreamAsync(image.FileName);
        return File(stream, image.ContentType);
    }

    [HttpGet("profilepicture/image/{id}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 2592000, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetProfilePicture(Guid id)
    {
        var image = await _context.BlobStorageItem.FirstOrDefaultAsync(x => x.Id == id);
        if (image == null) return NotFound();

        var stream = await _blobAgent.OpenDownloadStreamAsync(image.FileName);
        return File(stream, image.ContentType);
    }

    [HttpGet("event/image/{id}/social-preview.jpg")]
    [AllowAnonymous]
    [ResponseCache(Duration = 2592000, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetSocialPreview(Guid id)
    {
        var image = await _context.BlobStorageItem.FirstOrDefaultAsync(x => x.Id == id);
        if (image == null) return NotFound();

        // Add binary headers to assist social crawler caching and verification
        if (Response != null)
        {
            Response.Headers.AcceptRanges = "bytes";
            Response.Headers.ETag = $"\"{id:N}\"";
            Response.Headers.LastModified = image.CreatedOn.ToUniversalTime().ToString("R");
        }

        using var stream = await _blobAgent.OpenDownloadStreamAsync(image.FileName);
        using var sourceImage = await Image.LoadAsync(stream);

        // Target aspect ratio 1.91:1 (Industry standard for Open Graph images)
        const double targetRatio = 1.91;
        double currentRatio = (double)sourceImage.Width / sourceImage.Height;

        if (Math.Abs(currentRatio - targetRatio) > 0.05)
        {
            // Perform center crop to match target ratio
            int newWidth = sourceImage.Width;
            int newHeight = sourceImage.Height;

            if (currentRatio > targetRatio)
            {
                newWidth = Math.Max(1, (int)(sourceImage.Height * targetRatio));
            }
            else
            {
                newHeight = Math.Max(1, (int)(sourceImage.Width / targetRatio));
            }

            sourceImage.Mutate(x => x.Crop(new Rectangle(
                (sourceImage.Width - newWidth) / 2,
                (sourceImage.Height - newHeight) / 2,
                newWidth,
                newHeight)));
        }

        // Standardize dimensions: Minimum 300px for WhatsApp, maximum 1200px for performance.
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
        // WhatsApp is sensitive to file size (ideally < 300KB). 
        // Quality 75 provides a great balance for social previews.
        await sourceImage.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 75 });
        
        outputStream.Position = 0;
        return File(outputStream, "image/jpeg");
    }
}
