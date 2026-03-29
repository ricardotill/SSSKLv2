using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Data;
using SSSKLv2.Dto;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Dto.Api.v1;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace SSSKLv2.Controllers.v1;

[Authorize]
[Route("v1/[controller]")]
[ApiController]
public class ApplicationUserController : ControllerBase
{
    private readonly IApplicationUserService _applicationUserService;
    private readonly ILogger<ApplicationUserController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationUserController(IApplicationUserService applicationUserService, ILogger<ApplicationUserController> logger, UserManager<ApplicationUser> userManager)
    {
        _applicationUserService = applicationUserService;
        _logger = logger;
        _userManager = userManager;
    }

    private static ApplicationUserDto MapToDto(ApplicationUser u) => new ApplicationUserDto
    {
        Id = u.Id,
        UserName = u.UserName ?? string.Empty,
        FullName = u.FullName,
        Saldo = u.Saldo,
        LastOrdered = u.LastOrdered,
        ProfilePictureUrl = u.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{u.ProfileImageId}" : null
    };

    // New: map to the more detailed DTO (does NOT include password)
    private static ApplicationUserDetailedDto MapToDetailedDto(ApplicationUser u, IList<string> roles) => new ApplicationUserDetailedDto
    {
        Id = u.Id,
        UserName = u.UserName ?? string.Empty,
        Email = u.Email,
        EmailConfirmed = u.EmailConfirmed,
        PhoneNumber = u.PhoneNumber,
        PhoneNumberConfirmed = u.PhoneNumberConfirmed,
        Name = u.Name,
        Surname = u.Surname,
        FullName = u.FullName,
        Saldo = u.Saldo,
        LastOrdered = u.LastOrdered,
        ProfilePictureUrl = u.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{u.ProfileImageId}" : null,
        Roles = roles
    };

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApplicationUserDto>>> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        _logger.LogInformation("{Controller}: Get all users", nameof(ApplicationUserController));
        
        var list = await _applicationUserService.GetAllUsers(skip, take);
        var totalCount = await _applicationUserService.GetCount();
        var dtoItems = list.Select(MapToDto).ToList();
        
        return Ok(new PaginationObject<ApplicationUserDto>()
        {
            Items = dtoItems,
            TotalCount = totalCount
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public async Task<ActionResult<IEnumerable<ApplicationUserDto>>> GetAllAdmin([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        _logger.LogInformation("{Controller}: Get all users for admin", nameof(ApplicationUserController));
        
        var list = await _applicationUserService.GetAllUsersAdmin(skip, take);
        var totalCount = await _applicationUserService.GetCountAdmin();
        var dtoItems = list.Select(MapToDto).ToList();
        
        return Ok(new PaginationObject<ApplicationUserDto>()
        {
            Items = dtoItems,
            TotalCount = totalCount
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationUserDetailedDto>> GetById(string id)
    {
        _logger.LogInformation("{Controller}: Get user by id {Id}", nameof(ApplicationUserController), id);
        try
        {
            var user = await _applicationUserService.GetUserById(id);
            var roles = await _applicationUserService.GetUserRoles(user.Id);
            return Ok(MapToDetailedDto(user, roles));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("by-username/{username}")]
    public async Task<ActionResult<ApplicationUserDetailedDto>> GetByUsername(string username)
    {
        _logger.LogInformation("{Controller}: Get user by username {Username}", nameof(ApplicationUserController), username);
        try
        {
            var user = await _applicationUserService.GetUserByUsername(username);
            var roles = await _applicationUserService.GetUserRoles(user.Id);
            return Ok(MapToDetailedDto(user, roles));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }



    // GET v1/applicationuser/me
    [HttpGet("me")]
    public async Task<ActionResult<ApplicationUserDetailedDto>> GetCurrent()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }

        try
        {
            var user = await _applicationUserService.GetUserByUsername(username);
            var roles = await _applicationUserService.GetUserRoles(user.Id);
            return Ok(MapToDetailedDto(user, roles));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    // GET v1/applicationuser/me/personaldata - download personal data annotated with [PersonalData]
    [HttpPost("me/personaldata")]
    public async Task<IActionResult> DownloadPersonalData()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var userId = await _userManager.GetUserIdAsync(user);
        _logger.LogInformation("User with ID '{UserId}' requested personal data.", userId);

        var personalData = new Dictionary<string, string>();
        var personalDataProps = typeof(ApplicationUser).GetProperties().Where(
            prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));

        foreach (var p in personalDataProps)
        {
            personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
        }


        var fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData);

        Response.Headers.TryAdd("Content-Disposition", "attachment; filename=PersonalData.json");
        return File(fileBytes, contentType: "application/json", fileDownloadName: "PersonalData.json");
    }

    // PUT v1/applicationuser/{id} - update user (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ApplicationUserUpdateDto? dto)
    {
        if (dto == null) return BadRequest();
        if (!string.IsNullOrWhiteSpace(dto.Id) && dto.Id != id) return BadRequest("Id in payload does not match route id");
        // Username cannot be changed
        dto.UserName = null;

        try
        {
            // Fetch existing first so NotFound from the service is returned when appropriate
            var existing = await _applicationUserService.GetUserById(id);

            // Keep explicit owner/admin guard so direct action calls (unit tests) enforce authorization consistently.
            var currentUsername = User.Identity?.Name;
            var isAdmin = User.IsInRole("Admin");
            var isOwner = !string.IsNullOrWhiteSpace(currentUsername) &&
                          string.Equals(currentUsername, existing.UserName, StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && !isOwner) return Forbid();

            var updated = await _applicationUserService.UpdateUser(id, dto);
            var roles = await _applicationUserService.GetUserRoles(updated.Id);
            return Ok(MapToDetailedDto(updated, roles));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            // user manager errors surfaced as InvalidOperationException in service
            _logger.LogWarning(ex, "Failed to update user {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // PUT v1/applicationuser/me - update current user (only phone, name, surname)
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] ApplicationUserSelfUpdateDto? dto)
    {
        if (dto == null) return BadRequest();

        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        try
        {
            // Resolve current user
            var existing = await _applicationUserService.GetUserByUsername(username);

            // Map allowed fields to the existing update DTO and call the same service method
            var updateDto = new ApplicationUserUpdateDto
            {
                Id = existing.Id,
                PhoneNumber = dto.PhoneNumber,
                Name = dto.Name,
                Surname = dto.Surname
            };

            var updated = await _applicationUserService.UpdateUser(existing.Id, updateDto);
            var roles = await _applicationUserService.GetUserRoles(updated.Id);
            return Ok(MapToDetailedDto(updated, roles));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update current user {Username}", username);
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE v1/applicationuser/{id} - delete user (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _applicationUserService.DeleteUser(id);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to delete user {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE v1/applicationuser/me - delete currently logged in user
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        try
        {
            var existing = await _applicationUserService.GetUserByUsername(username);
            await _applicationUserService.DeleteUser(existing.Id);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to delete current user {Username}", username);
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST v1/applicationuser/me/profile-picture - upload profile picture
    [HttpPost("me/profile-picture")]
    public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile file)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        if (file == null || file.Length == 0) return BadRequest("File is empty");

        try
        {
            var user = await _applicationUserService.GetUserByUsername(username);
            using var stream = file.OpenReadStream();
            await _applicationUserService.UpdateProfilePictureAsync(user.Id, stream, file.ContentType);
            return Ok();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload profile picture for user {Username}", username);
            return BadRequest(new { error = "Failed to upload profile picture" });
        }
    }

    // DELETE v1/applicationuser/me/profile-picture - delete profile picture
    [HttpDelete("me/profile-picture")]
    public async Task<IActionResult> DeleteProfilePicture()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        try
        {
            var user = await _applicationUserService.GetUserByUsername(username);
            await _applicationUserService.DeleteProfilePictureAsync(user.Id);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete profile picture for user {Username}", username);
            return BadRequest(new { error = "Failed to delete profile picture" });
        }
    }

    // DELETE v1/applicationuser/{id}/profile-picture - delete profile picture (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}/profile-picture")]
    public async Task<IActionResult> DeleteUserProfilePicture(string id)
    {
        try
        {
            await _applicationUserService.DeleteProfilePictureAsync(id);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete profile picture for user {Id}", id);
            return BadRequest(new { error = "Failed to delete profile picture" });
        }
    }
}
