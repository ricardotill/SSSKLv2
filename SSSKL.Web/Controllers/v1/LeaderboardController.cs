using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKL.Application.Features.Leaderboards.Queries.GetLeaderboard;
using SSSKL.Contracts.Dto;

namespace SSSKL.Web.Controllers.v1;

[Route("v1/[controller]")]
[ApiController]
public class LeaderboardController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("{productId:guid}")]
    public Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetLeaderboard(Guid productId) =>
        Get(productId, LeaderboardPeriod.All);

    [HttpGet("monthly/{productId:guid}")]
    public Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetMonthlyLeaderboard(Guid productId) =>
        Get(productId, LeaderboardPeriod.Monthly);

    [HttpGet("12hour/{productId:guid}")]
    public Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> Get12HourlyLeaderboard(Guid productId) =>
        Get(productId, LeaderboardPeriod.TwelveHours);

    [HttpGet("12hour/live/{productId:guid}")]
    public Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> Get12HourlyLiveLeaderboard(Guid productId) =>
        Get(productId, LeaderboardPeriod.TwelveHoursLive);

    private async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> Get(Guid productId, LeaderboardPeriod period) =>
        Ok(await sender.Send(new GetLeaderboardQuery(productId, period)));
}
