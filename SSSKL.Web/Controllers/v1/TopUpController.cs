using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKL.Application.Features.TopUps.Commands.CreateTopUp;
using SSSKL.Application.Features.TopUps.Commands.DeleteTopUp;
using SSSKL.Application.Features.TopUps.Queries.GetPersonalTopUps;
using SSSKL.Application.Features.TopUps.Queries.GetTopUpById;
using SSSKL.Application.Features.TopUps.Queries.GetTopUps;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Web.Controllers.v1;

[Authorize]
[Route("v1/[controller]")]
[ApiController]
public class TopUpController(ISender sender) : ControllerBase
{
    // GET v1/topup
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<PaginationObject<TopUpDto>>> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        var username = User.Identity!.Name; // non-nullable per auth
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        return Ok(await sender.Send(new GetTopUpsQuery(skip, take)));
    }

    // GET v1/topup/personal
    [Authorize]
    [HttpGet("personal")]
    public async Task<ActionResult<PaginationObject<TopUpDto>>> GetPersonal([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        var username = User.Identity!.Name; // non-nullable per auth
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        return Ok(await sender.Send(new GetPersonalTopUpsQuery(username, skip, take)));
    }

    // GET v1/topup/{id}
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TopUpDto>> GetById(Guid id)
    {
        return Ok(await sender.Send(new GetTopUpByIdQuery(id)));
    }

    // POST v1/topup
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<TopUpDto>> Create([FromBody] TopUpCreateDto? dto)
    {
        if (dto == null) return BadRequest();

        if (ModelState.Values.SelectMany(v => v.Errors).Any())
        {
            return BadRequest(ModelState);
        }

        var topUp = await sender.Send(new CreateTopUpCommand(dto.UserName, dto.Saldo));
        return CreatedAtAction(nameof(GetById), new { id = topUp.Id }, topUp);
    }

    // DELETE v1/topup/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await sender.Send(new DeleteTopUpCommand(id));
        return NoContent();
    }
}
