using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SSSKLv2.Controllers.v1;
using SSSKLv2.Data;
using SSSKLv2.Dto;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Services.Interfaces;
using System.Security.Claims;
using SSSKLv2.Data.DAL.Exceptions;

namespace SSSKLv2.Test.Controllers;

[TestClass]
[DoNotParallelize]
public class AchievementControllerTests
{
    private IAchievementService _mockService = null!;
    private AchievementController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _mockService = Substitute.For<IAchievementService>();
        _sut = new AchievementController(_mockService);
    }

    [TestMethod]
    public async Task GetAll_ReturnsOkWithItems()
    {
        var items = new List<Achievement>
        {
            new Achievement { Id = Guid.NewGuid(), Name = "A1" },
            new Achievement { Id = Guid.NewGuid(), Name = "A2" }
        };
        // Controller calls GetAchievements(skip, take) and GetCount()
        _mockService.GetAchievements(Arg.Any<int>(), Arg.Any<int>()).Returns(Task.FromResult((IList<Achievement>)items));
        _mockService.GetCount().Returns(items.Count);

        var result = await _sut.GetAll();

        var expected = items.Select(a => new AchievementResponseDto { Id = a.Id, Name = a.Name, Description = a.Description ?? string.Empty, AutoAchieve = a.AutoAchieve, Action = a.Action, ComparisonOperator = a.ComparisonOperator, ComparisonValue = a.ComparisonValue, Image = null }).ToList();

        // Expect a pagination object with items and total count
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(new PaginationObject<AchievementResponseDto> { Items = expected, TotalCount = items.Count });
    }

    [TestMethod]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var achievement = new Achievement { Id = id, Name = "Found" };
        _mockService.GetAchievementById(id).Returns(Task.FromResult(achievement));

        var result = await _sut.GetById(id);

        var expected = new AchievementResponseDto { Id = achievement.Id, Name = achievement.Name, Description = achievement.Description ?? string.Empty, AutoAchieve = achievement.AutoAchieve, Action = achievement.Action, ComparisonOperator = achievement.ComparisonOperator, ComparisonValue = achievement.ComparisonValue, Image = null };
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _mockService.GetAchievementById(id).Returns(Task.FromException<Achievement>(new NotFoundException("Achievement not found")));

        var result = await _sut.GetById(id);

        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task Update_WithValidDto_CallsUpdateAndReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var existing = new Achievement { Id = id, Name = "Old" };
        _mockService.GetAchievementById(id).Returns(Task.FromResult(existing));

        var dto = new AchievementUpdateDto
        {
            Id = id,
            Name = "New",
            Description = "d",
            AutoAchieve = false,
            Action = Achievement.ActionOption.None,
            ComparisonOperator = Achievement.ComparisonOperatorOption.None,
            ComparisonValue = 0
        };

        var result = await _sut.Update(dto);

        result.Should().BeOfType<NoContentResult>();
        await _mockService.Received(1).UpdateAchievement(Arg.Any<Achievement>());
    }

    [TestMethod]
    public async Task Delete_CallsDeleteAndReturnsNoContent()
    {
        var id = Guid.NewGuid();

        var result = await _sut.Delete(id);

        result.Should().BeOfType<NoContentResult>();
        await _mockService.Received(1).DeleteAchievement(id);
    }

    [TestMethod]
    public async Task GetPersonal_ReturnsOk()
    {
        var username = "user1";
        var list = new List<AchievementListingDto> { new AchievementListingDto("n","d",null,null,false) };
        _mockService.GetPersonalAchievementsByUsername(username).Returns(list);

        // Set authenticated user on controller
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, "TestAuth"))
            }
        };

        var result = await _sut.GetPersonal();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(list);
    }

    [TestMethod]
    public async Task GetPersonalEntries_ReturnsOk()
    {
        var userId = "user1";
        var entries = new List<AchievementEntry> { new AchievementEntry { Id = Guid.NewGuid(), Achievement = new Achievement { Id = Guid.NewGuid(), Name = "ach1" } } };
        _mockService.GetPersonalAchievementEntries(userId).Returns(entries);

        var result = await _sut.GetPersonalEntries(userId);

        var expected = entries.Select(e => new AchievementEntryDto { Id = e.Id, AchievementId = e.Achievement.Id, AchievementName = e.Achievement.Name, AchievementDescription = e.Achievement.Description ?? string.Empty, DateAdded = e.CreatedOn, ImageUrl = e.Achievement?.Image?.Uri, HasSeen = e.HasSeen, UserId = e.User?.Id });
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task GetPersonalEntries_ByUsername_ReturnsOk()
    {
        var username = "user1";
        var entries = new List<AchievementEntry> { new AchievementEntry { Id = Guid.NewGuid(), HasSeen = false, Achievement = new Achievement { Id = Guid.NewGuid(), Name = "ach1" } } };
        _mockService.GetPersonalAchievementEntries(username).Returns(entries);

        var result = await _sut.GetPersonalEntries(username);

        var expected = entries.Select(e => new AchievementEntryDto { Id = e.Id, AchievementId = e.Achievement.Id, AchievementName = e.Achievement.Name, AchievementDescription = e.Achievement.Description ?? string.Empty, DateAdded = e.CreatedOn, ImageUrl = e.Achievement?.Image?.Uri, HasSeen = e.HasSeen, UserId = e.User?.Id });
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task DeleteEntries_WithEmpty_ReturnsBadRequest()
    {
        var result = await _sut.DeleteEntries(new List<Guid>());
        result.Should().BeOfType<BadRequestResult>();
    }

    [TestMethod]
    public async Task DeleteEntries_WithIds_CallsDeleteRange_ReturnsNoContent()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var result = await _sut.DeleteEntries(ids);

        result.Should().BeOfType<NoContentResult>();
        await _mockService.Received(1).DeleteAchievementEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }

    [TestMethod]
    public async Task AwardToUser_WhenAlreadyAwarded_ReturnsConflict()
    {
        var userId = "u1";
        var achievementId = Guid.NewGuid();
        _mockService.AwardAchievementToUser(userId, achievementId).Returns(false);

        var result = await _sut.AwardToUser(userId, achievementId);

        result.Should().BeOfType<ConflictResult>();
    }

    [TestMethod]
    public async Task AwardToUser_WhenSucceeds_ReturnsOkTrue()
    {
        var userId = "u1";
        var achievementId = Guid.NewGuid();
        _mockService.AwardAchievementToUser(userId, achievementId).Returns(true);

        var result = await _sut.AwardToUser(userId, achievementId);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [TestMethod]
    public async Task AwardToAll_ReturnsCount()
    {
        var achievementId = Guid.NewGuid();
        _mockService.AwardAchievementToAllUsers(achievementId).Returns(5);

        var result = await _sut.AwardToAll(achievementId);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(5);
    }

    [TestMethod]
    public async Task GetPersonalEntries_Personal_ReturnsOk()
    {
        var username = "user1";
        var entries = new List<AchievementEntry> { new AchievementEntry { Id = Guid.NewGuid(), Achievement = new Achievement { Id = Guid.NewGuid(), Name = "ach1" } } };
        _mockService.GetPersonalAchievementEntriesByUsername(username).Returns(entries);

        // Set authenticated user on controller
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, "TestAuth"))
            }
        };

        var result = await _sut.GetPersonalEntries();

        var expected = entries.Select(e => new AchievementEntryDto { Id = e.Id, AchievementId = e.Achievement.Id, AchievementName = e.Achievement.Name, AchievementDescription = e.Achievement.Description ?? string.Empty, DateAdded = e.CreatedOn, ImageUrl = e.Achievement?.Image?.Uri, HasSeen = e.HasSeen, UserId = e.User?.Id });
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }
}
