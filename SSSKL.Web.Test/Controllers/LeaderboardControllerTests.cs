using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Application.Features.Leaderboards.Queries.GetLeaderboard;
using SSSKL.Contracts.Dto;
using SSSKL.Domain.Exceptions;
using SSSKL.Web.Controllers.v1;

namespace SSSKL.Web.Test.Controllers;

[TestClass]
public class LeaderboardControllerTests
{
    private ISender _sender = null!;
    private LeaderboardController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _sender = Substitute.For<ISender>();
        _sut = new LeaderboardController(_sender);
    }

    private IReadOnlyList<LeaderboardEntryDto> Expect(Guid productId, LeaderboardPeriod period)
    {
        IReadOnlyList<LeaderboardEntryDto> items = [new LeaderboardEntryDto { FullName = "A B", ProductName = "Cola", Amount = 3, Position = 1 }];
        _sender.Send(new GetLeaderboardQuery(productId, period), Arg.Any<CancellationToken>()).Returns(items);
        return items;
    }

    [TestMethod]
    public async Task GetLeaderboard_ReturnsAllTimeLeaderboard()
    {
        var productId = Guid.NewGuid();
        var items = Expect(productId, LeaderboardPeriod.All);

        var result = await _sut.GetLeaderboard(productId);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(items);
    }

    [TestMethod]
    public async Task GetLeaderboard_WhenProductNotFound_PropagatesNotFoundException()
    {
        var productId = Guid.NewGuid();
        _sender.Send(Arg.Any<GetLeaderboardQuery>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("Product not found"));

        var act = () => _sut.GetLeaderboard(productId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task GetMonthlyLeaderboard_ReturnsMonthlyLeaderboard()
    {
        var productId = Guid.NewGuid();
        var items = Expect(productId, LeaderboardPeriod.Monthly);

        var result = await _sut.GetMonthlyLeaderboard(productId);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(items);
    }

    [TestMethod]
    public async Task Get12HourlyLeaderboard_ReturnsTwelveHourLeaderboard()
    {
        var productId = Guid.NewGuid();
        var items = Expect(productId, LeaderboardPeriod.TwelveHours);

        var result = await _sut.Get12HourlyLeaderboard(productId);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(items);
    }

    [TestMethod]
    public async Task Get12HourlyLiveLeaderboard_ReturnsLiveLeaderboard()
    {
        var productId = Guid.NewGuid();
        var items = Expect(productId, LeaderboardPeriod.TwelveHoursLive);

        var result = await _sut.Get12HourlyLiveLeaderboard(productId);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(items);
    }
}
