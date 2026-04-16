using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Dto;
using SSSKLv2.Services.Interfaces;
using System.Security.Claims;

namespace SSSKLv2.Controllers.v1;

[Authorize]
[Route("v1/[controller]")]
[ApiController]
public class ReactionsController : ControllerBase
{
    private readonly IReactionService _reactionService;

    public ReactionsController(IReactionService reactionService)
    {
        _reactionService = reactionService;
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle([FromBody] ToggleReactionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            await _reactionService.ToggleReaction(request.TargetId, request.TargetType, request.Content, userId);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpGet("{targetId:guid}/{targetType}")]
    public async Task<ActionResult<IEnumerable<ReactionDto>>> Get(Guid targetId, string targetType)
    {
        try
        {
            var reactions = await _reactionService.GetReactionsForTarget(targetId, targetType);
            return Ok(reactions);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("timeline")]
    public async Task<ActionResult<IEnumerable<ReactionDto>>> GetTimeline([FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        var reactions = await _reactionService.GetTimeline(skip, take);
        return Ok(reactions);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        try
        {
            await _reactionService.DeleteReaction(id, userId, isAdmin);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }
}
