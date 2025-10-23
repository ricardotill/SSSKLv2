using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Data;
using SSSKLv2.Dto;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Controllers.v1;

[Authorize]
[Route("v1/[controller]")]
[ApiController]
public class ApplicationUserController : ControllerBase
{
    private readonly IApplicationUserService _applicationUserService;
    private readonly ILogger<ApplicationUserController> _logger;

    public ApplicationUserController(IApplicationUserService applicationUserService, ILogger<ApplicationUserController> logger)
    {
        _applicationUserService = applicationUserService;
        _logger = logger;
    }

    private static ApplicationUserDto MapToDto(ApplicationUser u) => new ApplicationUserDto
    {
        Id = u.Id,
        UserName = u.UserName ?? string.Empty,
        FullName = u.FullName ?? string.Empty,
        Saldo = u.Saldo,
        LastOrdered = u.LastOrdered
    };

    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationUserDto>> GetById(string id)
    {
        _logger.LogInformation("{Controller}: Get user by id {Id}", nameof(ApplicationUserController), id);
        try
        {
            var user = await _applicationUserService.GetUserById(id);
            return Ok(MapToDto(user));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("by-username/{username}")]
    public async Task<ActionResult<ApplicationUserDto>> GetByUsername(string username)
    {
        _logger.LogInformation("{Controller}: Get user by username {Username}", nameof(ApplicationUserController), username);
        try
        {
            var user = await _applicationUserService.GetUserByUsername(username);
            return Ok(MapToDto(user));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IList<ApplicationUserDto>>> GetAll()
    {
        _logger.LogInformation("{Controller}: Get all users", nameof(ApplicationUserController));
        var users = await _applicationUserService.GetAllUsers();
        return Ok(users.Select(MapToDto).ToList());
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("obscured")]
    public async Task<ActionResult<IEnumerable<ApplicationUserDto>>> GetAllObscured()
    {
        _logger.LogInformation("{Controller}: Get all users obscured", nameof(ApplicationUserController));
        var users = await _applicationUserService.GetAllUsersObscured();
        return Ok(users.Select(MapToDto).ToList());
    }

    [AllowAnonymous]
    [HttpGet("leaderboard/{productId:guid}")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetLeaderboard(Guid productId)
    {
        _logger.LogInformation("{Controller}: Get all leaderboard for product {ProductId}", nameof(ApplicationUserController), productId);
        try
        {
            var result = await _applicationUserService.GetAllLeaderboard(productId);
            return Ok(result);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("leaderboard/monthly/{productId:guid}")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetMonthlyLeaderboard(Guid productId)
    {
        _logger.LogInformation("{Controller}: Get monthly leaderboard for product {ProductId}", nameof(ApplicationUserController), productId);
        try
        {
            var result = await _applicationUserService.GetMonthlyLeaderboard(productId);
            return Ok(result);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("leaderboard/12hour/{productId:guid}")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> Get12HourlyLeaderboard(Guid productId)
    {
        _logger.LogInformation("{Controller}: Get 12-hour leaderboard for product {ProductId}", nameof(ApplicationUserController), productId);
        try
        {
            var result = await _applicationUserService.Get12HourlyLeaderboard(productId);
            return Ok(result);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("leaderboard/12hour/live/{productId:guid}")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> Get12HourlyLiveLeaderboard(Guid productId)
    {
        _logger.LogInformation("{Controller}: Get 12-hour live leaderboard for product {ProductId}", nameof(ApplicationUserController), productId);
        try
        {
            var result = await _applicationUserService.Get12HourlyLiveLeaderboard(productId);
            return Ok(result);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    // GET v1/applicationuser/me
    [HttpGet("me")]
    public async Task<ActionResult<ApplicationUserDto>> GetCurrent()
    {
        var username = User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }

        try
        {
            var user = await _applicationUserService.GetUserByUsername(username);
            return Ok(MapToDto(user));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}