using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Agents;
using SSSKLv2.Data;
using Microsoft.EntityFrameworkCore;

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
}
