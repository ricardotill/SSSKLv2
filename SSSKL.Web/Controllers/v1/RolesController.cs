using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKL.Application.Features.RoleManagement;
using SSSKL.Application.Features.RoleManagement.Commands.CreateRole;
using SSSKL.Application.Features.RoleManagement.Commands.DeleteRole;
using SSSKL.Application.Features.RoleManagement.Commands.UpdateRole;
using SSSKL.Application.Features.RoleManagement.Queries.GetRoles;
using SSSKL.Contracts.Dto.Api;
using SSSKL.Domain.Constants;

namespace SSSKL.Web.Controllers.v1;

[Route("v1/[controller]")]
[ApiController]
public class RolesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.User}")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAll()
    {
        return Ok(await sender.Send(new GetRolesQuery(IncludeProtected: false)));
    }

    [HttpGet("admin")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllAdmin()
    {
        return Ok(await sender.Send(new GetRolesQuery(IncludeProtected: true)));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Role name cannot be empty.");

        var result = await sender.Send(new CreateRoleCommand(dto.Name));
        if (!result.Succeeded) return IdentityProblem(result);

        return CreatedAtAction(nameof(GetAll), result.Role);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<RoleDto>> Update(string id, [FromBody] CreateRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Role name cannot be empty.");

        var result = await sender.Send(new UpdateRoleCommand(id, dto.Name));
        if (!result.Succeeded) return IdentityProblem(result);

        return Ok(result.Role);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await sender.Send(new DeleteRoleCommand(id));
        if (!result.Succeeded) return IdentityProblem(result);

        return NoContent();
    }

    private ActionResult IdentityProblem(RoleResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }
        return ValidationProblem(ModelState);
    }
}
