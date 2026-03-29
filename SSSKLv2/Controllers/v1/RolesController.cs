using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Dto.Api;
using SSSKLv2.Data.Constants;

namespace SSSKLv2.Controllers.v1;

[Route("v1/[controller]")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<RolesController> _logger;

    // These roles are required for system functioning and cannot be deleted.
    private static readonly string[] ProtectedRoles = Roles.AllProtected;

    public RolesController(RoleManager<IdentityRole> roleManager, ILogger<RolesController> logger)
    {
        _roleManager = roleManager;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.User}")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAll()
    {
        var roles = await _roleManager.Roles
            .Where(r => !ProtectedRoles.Contains(r.Name))
            .ToListAsync();
            
        return Ok(roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name ?? string.Empty
        }));
    }

    [HttpGet("admin")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllAdmin()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        return Ok(roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name ?? string.Empty
        }));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Role name cannot be empty.");

        var roleExists = await _roleManager.RoleExistsAsync(dto.Name);
        if (roleExists)
            return Conflict($"Role '{dto.Name}' already exists.");

        var newRole = new IdentityRole(dto.Name);
        var result = await _roleManager.CreateAsync(newRole);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return ValidationProblem(ModelState);
        }

        return CreatedAtAction(nameof(GetAll), new RoleDto { Id = newRole.Id, Name = dto.Name });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<RoleDto>> Update(string id, [FromBody] CreateRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Role name cannot be empty.");

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound("Role not found.");

        if (ProtectedRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
        {
            return Forbid($"Cannot update protected system role '{role.Name}'.");
        }

        var roleExists = await _roleManager.RoleExistsAsync(dto.Name);
        if (roleExists && !string.Equals(role.Name, dto.Name, StringComparison.OrdinalIgnoreCase))
            return Conflict($"Role '{dto.Name}' already exists.");

        role.Name = dto.Name;
        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return ValidationProblem(ModelState);
        }

        return Ok(new RoleDto { Id = role.Id, Name = role.Name });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound("Role not found.");

        if (ProtectedRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
        {
            return Forbid($"Cannot delete protected system role '{role.Name}'.");
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return ValidationProblem(ModelState);
        }

        return NoContent();
    }
}
