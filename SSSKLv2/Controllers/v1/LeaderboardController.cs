using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Dto;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Data.DAL.Exceptions;

namespace SSSKLv2.Controllers.v1;

[Route("v1/[controller]")]
[ApiController]
public class LeaderboardController : ControllerBase
{
    private readonly IApplicationUserService _applicationUserService;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(IApplicationUserService applicationUserService, ILogger<LeaderboardController> logger)
    {
        _applicationUserService = applicationUserService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetLeaderboard(Guid productId)
    {
        _logger.LogInformation("{Controller}: Get all leaderboard for product {ProductId}", nameof(LeaderboardController), productId);
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

    [HttpGet("monthly/{productId:guid}")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetMonthlyLeaderboard(Guid productId)
    {
        _logger.LogInformation("{Controller}: Get monthly leaderboard for product {ProductId}", nameof(LeaderboardController), productId);
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

    [HttpGet("12hour/{productId:guid}")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> Get12HourlyLeaderboard(Guid productId)
    {
        _logger.LogInformation("{Controller}: Get 12-hour leaderboard for product {ProductId}", nameof(LeaderboardController), productId);
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

    [HttpGet("12hour/live/{productId:guid}")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> Get12HourlyLiveLeaderboard(Guid productId)
    {
        _logger.LogInformation("{Controller}: Get 12-hour live leaderboard for product {ProductId}", nameof(LeaderboardController), productId);
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
}
