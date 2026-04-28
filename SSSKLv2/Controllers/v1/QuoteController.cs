using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Controllers.v1;

[Route("v1/[controller]")]
[ApiController]
public class QuoteController(IQuoteService quoteService, ILogger<QuoteController> logger) : ControllerBase
{
    // GET v1/quote
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15, [FromQuery] string? targetUserId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        logger.LogInformation("{Controller}: Get quotes skip={Skip} take={Take} targetUserId={TargetUserId} for user {UserId}", nameof(QuoteController), skip, take, targetUserId, userId);
        
        try
        {
            var result = await quoteService.GetQuotesAsync(skip, take, userId, targetUserId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    // GET v1/quote/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        logger.LogInformation("{Controller}: Get quote {Id} for user {UserId}", nameof(QuoteController), id, userId);

        try
        {
            var quote = await quoteService.GetQuoteByIdAsync(id, userId);
            if (quote == null) return NotFound();
            return Ok(quote);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    // POST v1/quote
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] QuoteCreateDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userId == null) return Unauthorized();

        logger.LogInformation("{Controller}: Create quote by user {UserId}", nameof(QuoteController), userId);

        try
        {
            var created = await quoteService.CreateQuoteAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    // PUT v1/quote/{id}
    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] QuoteUpdateDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userId == null) return Unauthorized();

        logger.LogInformation("{Controller}: Update quote {Id} by user {UserId}", nameof(QuoteController), id, userId);

        try
        {
            var updated = await quoteService.UpdateQuoteAsync(id, dto, userId);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    // DELETE v1/quote/{id}
    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userId == null) return Unauthorized();

        logger.LogInformation("{Controller}: Delete quote {Id} by user {UserId}", nameof(QuoteController), id, userId);

        try
        {
            var success = await quoteService.DeleteQuoteAsync(id, userId);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    // POST v1/quote/{id}/vote
    [Authorize]
    [HttpPost("{id:guid}/vote")]
    public async Task<IActionResult> ToggleVote(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userId == null) return Unauthorized();

        logger.LogInformation("{Controller}: Toggle vote for quote {Id} by user {UserId}", nameof(QuoteController), id, userId);

        try
        {
            var hasVoted = await quoteService.ToggleVoteAsync(id, userId);
            return Ok(hasVoted);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }
}
