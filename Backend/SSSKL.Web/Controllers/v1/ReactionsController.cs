using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKL.Application.Features.Reactions.Commands.DeleteReaction;
using SSSKL.Application.Features.Reactions.Commands.ToggleReaction;
using SSSKL.Application.Features.Reactions.Queries.GetReactions;
using SSSKL.Application.Features.Reactions.Queries.GetReactionTimeline;
using SSSKL.Contracts.Dto;

namespace SSSKL.Web.Controllers.v1;

[Authorize]
[Route("v1/[controller]")]
[ApiController]
public class ReactionsController(ISender sender) : ControllerBase
{
    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle([FromBody] ToggleReactionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await sender.Send(new ToggleReactionCommand(request.TargetId, request.TargetType, request.Content, userId));
        return Ok();
    }

    [AllowAnonymous]
    [HttpGet("{targetId:guid}/{targetType}")]
    public async Task<ActionResult<IEnumerable<ReactionDto>>> Get(Guid targetId, string targetType)
    {
        return Ok(await sender.Send(new GetReactionsQuery(targetId, targetType)));
    }

    [HttpGet("timeline")]
    public async Task<ActionResult<IEnumerable<ReactionDto>>> GetTimeline([FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        return Ok(await sender.Send(new GetReactionTimelineQuery(skip, take)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await sender.Send(new DeleteReactionCommand(id, userId, User.IsInRole("Admin")));
        return NoContent();
    }
}
