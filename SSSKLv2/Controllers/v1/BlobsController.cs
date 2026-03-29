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
    public async Task<IActionResult> GetEventImage(Guid id)
    {
        var image = await _context.BlobStorageItem.FirstOrDefaultAsync(x => x.Id == id);
        if (image == null) return NotFound();

        var stream = await _blobAgent.OpenDownloadStreamAsync(image.FileName);
        return File(stream, image.ContentType);
    }

    [HttpGet("achievement/image/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAchievementImage(Guid id)
    {
        var image = await _context.BlobStorageItem.FirstOrDefaultAsync(x => x.Id == id);
        if (image == null) return NotFound();

        var stream = await _blobAgent.OpenDownloadStreamAsync(image.FileName);
        return File(stream, image.ContentType);
    }

    [HttpGet("profilepicture/image/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProfilePicture(Guid id)
    {
        var image = await _context.BlobStorageItem.FirstOrDefaultAsync(x => x.Id == id);
        if (image == null) return NotFound();

        var stream = await _blobAgent.OpenDownloadStreamAsync(image.FileName);
        return File(stream, image.ContentType);
    }

    [HttpGet("event/image/{id}/social-preview")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSocialPreview(Guid id)
    {
        var image = await _context.BlobStorageItem.FirstOrDefaultAsync(x => x.Id == id);
        if (image == null) return NotFound();

        using var stream = await _blobAgent.OpenDownloadStreamAsync(image.FileName);
        using var sourceImage = await Image.LoadAsync(stream);

        // 1. Aspect Ratio check (max 4:1)
        double aspectRatio = (double)sourceImage.Width / sourceImage.Height;
        if (aspectRatio > 4.0)
        {
            // Crop width to fit 4:1
            int newWidth = sourceImage.Height * 4;
            sourceImage.Mutate(x => x.Crop(new Rectangle((sourceImage.Width - newWidth) / 2, 0, newWidth, sourceImage.Height)));
        }

        // 2. Minimum width (300px)
        if (sourceImage.Width < 300)
        {
            sourceImage.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(300, 0),
                Mode = ResizeMode.Max
            }));
        }

        var outputStream = new MemoryStream();
        // 3. Compression and size limit
        // 85 quality is a good balance. For a 1200x630 (typical OG size) image, this should be ~100-200KB.
        await sourceImage.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 85 });
        
        outputStream.Position = 0;
        return File(outputStream, "image/jpeg");
    }
}
