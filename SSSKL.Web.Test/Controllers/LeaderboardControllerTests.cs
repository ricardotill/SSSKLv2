using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Microsoft.Extensions.Logging;
using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Exceptions;
using SSSKL.Web.Controllers.v1;

namespace SSSKL.Web.Test.Controllers;

[TestClass]
public class LeaderboardControllerTests
{
    private IApplicationUserService _mockService = null!;
    private LeaderboardController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _mockService = Substitute.For<IApplicationUserService>();
        var logger = Substitute.For<ILogger<LeaderboardController>>();
        _sut = new LeaderboardController(_mockService, logger);
    }

    [TestMethod]
    public async Task GetLeaderboard_ReturnsOkWithItems()
    {
        var productId = Guid.NewGuid();
        var entries = new List<LeaderboardEntryDto>
        {
            new LeaderboardEntryDto { Amount = 5, FullName = "User1", ProductName = "Product" }
        };
        _mockService.GetAllLeaderboard(productId).Returns(Task.FromResult((IEnumerable<LeaderboardEntryDto>)entries));

        var result = await _sut.GetLeaderboard(productId);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(entries);
    }

    [TestMethod]
    public async Task GetLeaderboard_WhenProductNotFound_ReturnsNotFound()
    {
        var productId = Guid.NewGuid();
        _mockService.GetAllLeaderboard(productId).Returns(Task.FromException<IEnumerable<LeaderboardEntryDto>>(new NotFoundException("Product not found")));

        var result = await _sut.GetLeaderboard(productId);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task GetMonthlyLeaderboard_ReturnsOkWithItems()
    {
        var productId = Guid.NewGuid();
        var entries = new List<LeaderboardEntryDto>
        {
            new LeaderboardEntryDto { Amount = 10, FullName = "MonthlyUser", ProductName = "Product" }
        };
        _mockService.GetMonthlyLeaderboard(productId).Returns(Task.FromResult((IEnumerable<LeaderboardEntryDto>)entries));

        var result = await _sut.GetMonthlyLeaderboard(productId);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(entries);
    }

    [TestMethod]
    public async Task Get12HourlyLeaderboard_ReturnsOkWithItems()
    {
        var productId = Guid.NewGuid();
        var entries = new List<LeaderboardEntryDto>
        {
            new LeaderboardEntryDto { Amount = 3, FullName = "HourlyUser", ProductName = "Product" }
        };
        _mockService.Get12HourlyLeaderboard(productId).Returns(Task.FromResult((IEnumerable<LeaderboardEntryDto>)entries));

        var result = await _sut.Get12HourlyLeaderboard(productId);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(entries);
    }

    [TestMethod]
    public async Task Get12HourlyLiveLeaderboard_ReturnsOkWithItems()
    {
        var productId = Guid.NewGuid();
        var entries = new List<LeaderboardEntryDto>
        {
            new LeaderboardEntryDto { Amount = 1, FullName = "LiveUser", ProductName = "Product" }
        };
        _mockService.Get12HourlyLiveLeaderboard(productId).Returns(Task.FromResult((IEnumerable<LeaderboardEntryDto>)entries));

        var result = await _sut.Get12HourlyLiveLeaderboard(productId);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(entries);
    }
}
