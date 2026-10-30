using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKL.Application.Features.Quotes.Commands.CreateQuote;
using SSSKL.Application.Features.Quotes.Commands.DeleteQuote;
using SSSKL.Application.Features.Quotes.Commands.ToggleQuoteVote;
using SSSKL.Application.Features.Quotes.Commands.UpdateQuote;
using SSSKL.Application.Features.Quotes.Queries.GetQuoteById;
using SSSKL.Application.Features.Quotes.Queries.GetQuotes;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Web.Controllers.v1;

[Authorize]
[Route("v1/[controller]")]
[ApiController]
public class QuoteController(ISender sender) : ControllerBase
{
    // GET v1/quote
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15, [FromQuery] string? targetUserId = null)
    {
        return Ok(await sender.Send(new GetQuotesQuery(skip, take, GetUserId(), targetUserId)));
    }

    // GET v1/quote/{id}
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Ok(await sender.Send(new GetQuoteByIdQuery(id, GetUserId())));
    }

    // POST v1/quote
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] QuoteCreateDto dto)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var created = await sender.Send(new CreateQuoteCommand(dto, userId));
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT v1/quote/{id}
    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] QuoteUpdateDto dto)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        return Ok(await sender.Send(new UpdateQuoteCommand(id, dto, userId)));
    }

    // DELETE v1/quote/{id}
    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        await sender.Send(new DeleteQuoteCommand(id, userId));
        return NoContent();
    }

    // POST v1/quote/{id}/vote
    [Authorize]
    [HttpPost("{id:guid}/vote")]
    public async Task<IActionResult> ToggleVote(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        return Ok(await sender.Send(new ToggleQuoteVoteCommand(id, userId)));
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
}
