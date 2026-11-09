using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SSSKL.Application.Features.Users;
using SSSKL.Application.Features.Users.Commands.DeleteProfilePicture;
using SSSKL.Application.Features.Users.Commands.DeleteUser;
using SSSKL.Application.Features.Users.Commands.UpdateProfilePicture;
using SSSKL.Application.Features.Users.Commands.UpdateUser;
using SSSKL.Application.Features.Users.Queries.GetPersonalData;
using SSSKL.Application.Features.Users.Queries.GetUserDetails;
using SSSKL.Application.Features.Users.Queries.GetUserProfile;
using SSSKL.Application.Features.Users.Queries.GetUsers;
using SSSKL.Application.Features.Users.Stats;
using SSSKL.Application.Util;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Web.Controllers.v1;

[Authorize]
[Route("v1/[controller]")]
[ApiController]
public class ApplicationUserController(ISender sender, ILogger<ApplicationUserController> logger) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PaginationObject<ApplicationUserDto>>> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        return Ok(await sender.Send(new GetUsersQuery(skip, take)));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public async Task<ActionResult<PaginationObject<ApplicationUserDto>>> GetAllAdmin([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        return Ok(await sender.Send(new GetUsersQuery(skip, take, ForAdmin: true)));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationUserDetailedDto>> GetById(string id)
    {
        return Ok(await sender.Send(new GetUserDetailsQuery(UserReference.ById(id))));
    }

    [Authorize]
    [HttpGet("{id}/profile")]
    public async Task<ActionResult<ApplicationUserDto>> GetProfileById(string id)
    {
        return Ok(await sender.Send(new GetUserProfileQuery(id)));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("by-username/{username}")]
    public async Task<ActionResult<ApplicationUserDetailedDto>> GetByUsername(string username)
    {
        return Ok(await sender.Send(new GetUserDetailsQuery(UserReference.ByUsername(username))));
    }

    // GET v1/applicationuser/me
    [HttpGet("me")]
    public async Task<ActionResult<ApplicationUserDetailedDto>> GetCurrent()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        return Ok(await sender.Send(new GetUserDetailsQuery(UserReference.ByUsername(username))));
    }

    // POST v1/applicationuser/me/personaldata - download personal data annotated with [PersonalData]
    [HttpPost("me/personaldata")]
    public async Task<IActionResult> DownloadPersonalData()
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return NotFound();

        var personalData = await sender.Send(new GetPersonalDataQuery(userId));
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

        return await UpdateUser(new UpdateUserCommand(UserReference.ById(id), dto, User.Identity?.Name, User.IsInRole("Admin")));
    }

    // PUT v1/applicationuser/me - update current user (only phone, name, surname)
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] ApplicationUserSelfUpdateDto? dto)
    {
        if (dto == null) return BadRequest();

        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        // Only the profile fields a user may change about themselves
        var changes = new ApplicationUserUpdateDto
        {
            PhoneNumber = dto.PhoneNumber,
            Name = dto.Name,
            Surname = dto.Surname,
            Description = dto.Description
        };

        return await UpdateUser(new UpdateUserCommand(UserReference.ByUsername(username), changes, username, IsAdmin: false));
    }

    private async Task<IActionResult> UpdateUser(UpdateUserCommand command)
    {
        try
        {
            return Ok(await sender.Send(command));
        }
        catch (InvalidOperationException ex)
        {
            // Identity errors, e.g. a duplicate email or a weak password
            logger.LogWarning(ex, "Failed to update user {User}", command.User);
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE v1/applicationuser/{id} - delete user (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(string id) => DeleteUser(UserReference.ById(id));

    // DELETE v1/applicationuser/me - delete currently logged in user
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        return await DeleteUser(UserReference.ByUsername(username));
    }

    private async Task<IActionResult> DeleteUser(UserReference user)
    {
        try
        {
            await sender.Send(new DeleteUserCommand(user));
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to delete user {User}", user);
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST v1/applicationuser/me/profile-picture - upload profile picture
    // Accepts a JSON base64 payload rather than multipart/form-data: iOS PWAs with an
    // active service worker are known to strip the body from multipart POST requests.
    [HttpPost("me/profile-picture")]
    public async Task<IActionResult> UploadProfilePicture([FromBody] Base64FileUploadDto? file)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        if (file == null || !Base64FileDecoder.TryDecode(file.Base64Content, out var bytes))
        {
            return BadRequest("File is empty");
        }

        try
        {
            using var stream = new MemoryStream(bytes);
            await sender.Send(new UpdateProfilePictureCommand(UserReference.ByUsername(username), stream));
            return Ok();
        }
        catch (ImageFormatException ex)
        {
            // Unknown or corrupt image data is a client error; anything else surfaces as a 500
            logger.LogWarning(ex, "Rejected invalid profile picture for user {Username}", username);
            return BadRequest(new { error = "Failed to upload profile picture" });
        }
    }

    // DELETE v1/applicationuser/me/profile-picture - delete profile picture
    [HttpDelete("me/profile-picture")]
    public async Task<IActionResult> DeleteProfilePicture()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        await sender.Send(new DeleteProfilePictureCommand(UserReference.ByUsername(username)));
        return NoContent();
    }

    // DELETE v1/applicationuser/{id}/profile-picture - delete profile picture (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}/profile-picture")]
    public async Task<IActionResult> DeleteUserProfilePicture(string id)
    {
        await sender.Send(new DeleteProfilePictureCommand(UserReference.ById(id)));
        return NoContent();
    }

    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetStats(string id)
    {
        return Ok(await sender.Send(new GetUserStatsQuery(id, GetUserId(), User.IsInRole("Admin"))));
    }

    // POST v1/applicationuser/stats/recalculate-all - starts a background job that recalculates stats for every user (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpPost("stats/recalculate-all")]
    public async Task<IActionResult> RecalculateAllStats()
    {
        var (job, started) = await sender.Send(new StartStatsRecalculationCommand(GetUserId() ?? string.Empty));
        if (!started)
        {
            return StatusCode(StatusCodes.Status409Conflict, job);
        }

        return AcceptedAtAction(nameof(GetRecalculateAllStatsStatus), new { jobId = job.Id }, job);
    }

    // GET v1/applicationuser/stats/recalculate-all/latest - status of the most recently started bulk recalculation job (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpGet("stats/recalculate-all/latest")]
    public async Task<IActionResult> GetLatestRecalculateAllStatsStatus()
    {
        var job = await sender.Send(new GetStatsRecalculationStatusQuery());
        if (job == null) return NotFound();
        return Ok(job);
    }

    // GET v1/applicationuser/stats/recalculate-all/{jobId} - status of a specific bulk recalculation job (Admin only)
    [Authorize(Roles = "Admin")]
    [HttpGet("stats/recalculate-all/{jobId:guid}")]
    public async Task<IActionResult> GetRecalculateAllStatsStatus(Guid jobId)
    {
        var job = await sender.Send(new GetStatsRecalculationStatusQuery(jobId));
        if (job == null) return NotFound();
        return Ok(job);
    }

    [Authorize]
    [HttpPost("{id}/stats/recalculate")]
    public async Task<IActionResult> RecalculateStats(string id)
    {
        var result = await sender.Send(new RecalculateUserStatsCommand(id, GetUserId(), User.IsInRole("Admin")));
        if (result.Stats == null)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                message = "Statistics can only be recalculated once every seven days.",
                nextAllowedAt = result.NextAllowedAt
            });
        }

        return Ok(result.Stats);
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
