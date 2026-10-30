using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Application.Features.Achievements.Commands.AwardAchievement;
using SSSKL.Application.Features.Achievements.Commands.CreateAchievement;
using SSSKL.Application.Features.Achievements.Commands.DeleteAchievement;
using SSSKL.Application.Features.Achievements.Commands.DeleteAchievementEntries;
using SSSKL.Application.Features.Achievements.Commands.MarkUnseenEntriesSeen;
using SSSKL.Application.Features.Achievements.Commands.UpdateAchievement;
using SSSKL.Application.Features.Achievements.Queries.GetAchievementById;
using SSSKL.Application.Features.Achievements.Queries.GetAchievementEarners;
using SSSKL.Application.Features.Achievements.Queries.GetAchievementEntries;
using SSSKL.Application.Features.Achievements.Queries.GetAchievementListing;
using SSSKL.Application.Features.Achievements.Queries.GetAchievements;
using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Exceptions;
using SSSKL.Web.Controllers.v1;

namespace SSSKL.Web.Test.Controllers;

[TestClass]
public class AchievementControllerTests
{
    private ISender _sender = null!;
    private AchievementController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _sender = Substitute.For<ISender>();
        _sut = new AchievementController(_sender);
    }

    private void SetUser(string username)
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, username)], "TestAuth"))
            }
        };
    }

    [TestMethod]
    public async Task GetAll_ReturnsOkWithItems()
    {
        var page = new PaginationObject<AchievementResponseDto> { Items = [new AchievementResponseDto { Id = Guid.NewGuid(), Name = "A1" }], TotalCount = 1 };
        _sender.Send(new GetAchievementsQuery(0, 15), Arg.Any<CancellationToken>()).Returns(page);

        var result = await _sut.GetAll();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
    }

    [TestMethod]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = new AchievementResponseDto { Id = id, Name = "Found" };
        _sender.Send(new GetAchievementByIdQuery(id), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.GetById(id);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(dto);
    }

    [TestMethod]
    public async Task GetById_WhenNotFound_PropagatesNotFoundException()
    {
        var id = Guid.NewGuid();
        _sender.Send(new GetAchievementByIdQuery(id), Arg.Any<CancellationToken>()).Throws(new NotFoundException("Achievement not found"));

        var act = () => _sut.GetById(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Update_WithoutNewImage_SendsCommandAndReturnsNoContent()
    {
        var dto = new AchievementUpdateDto { Id = Guid.NewGuid(), Name = "New" };

        var result = await _sut.Update(dto);

        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(Arg.Is<UpdateAchievementCommand>(c => c.Achievement == dto && c.NewImage == null), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Update_WithNewImage_SendsDecodedStream()
    {
        var dto = new AchievementUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "New",
            NewImage = new Base64FileUploadDto
            {
                FileName = "photo.png",
                ContentType = "image/png",
                Base64Content = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })
            }
        };

        var result = await _sut.Update(dto);

        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(Arg.Is<UpdateAchievementCommand>(c =>
            c.Achievement == dto && c.NewImage != null && c.NewImageContentType == "image/png"), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Update_WithUndecodableNewImage_ReturnsBadRequest()
    {
        var dto = new AchievementUpdateDto
        {
            Id = Guid.NewGuid(),
            NewImage = new Base64FileUploadDto { FileName = "x.png", ContentType = "image/png", Base64Content = "%%%" }
        };

        var result = await _sut.Update(dto);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _sender.DidNotReceive().Send(Arg.Any<UpdateAchievementCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Update_WhenHandlerRejectsImageType_ReturnsBadRequest()
    {
        _sender.Send(Arg.Any<UpdateAchievementCommand>(), Arg.Any<CancellationToken>()).Throws(new ArgumentException("Unsupported"));

        var result = await _sut.Update(new AchievementUpdateDto { Id = Guid.NewGuid() });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task Update_WhenNotFound_PropagatesNotFoundException()
    {
        _sender.Send(Arg.Any<UpdateAchievementCommand>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("Achievement not found"));

        var act = () => _sut.Update(new AchievementUpdateDto { Id = Guid.NewGuid() });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Delete_SendsCommandAndReturnsNoContent()
    {
        var id = Guid.NewGuid();

        var result = await _sut.Delete(id);

        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new DeleteAchievementCommand(id), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetPersonal_ReturnsListingForCurrentUser()
    {
        SetUser("testuser");
        IList<AchievementListingDto> list = [new AchievementListingDto(Guid.NewGuid(), "A1", "D", null, null, false, "Bronze", null, null)];
        _sender.Send(new GetAchievementListingQuery(Username: "testuser"), Arg.Any<CancellationToken>()).Returns(list);

        var result = await _sut.GetPersonal();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(list);
    }

    [TestMethod]
    public async Task GetPersonal_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await _sut.GetPersonal();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [TestMethod]
    public async Task GetAllForUser_ReturnsListingForUserId()
    {
        IList<AchievementListingDto> list = [];
        _sender.Send(new GetAchievementListingQuery(UserId: "u1"), Arg.Any<CancellationToken>()).Returns(list);

        var result = await _sut.GetAllForUser("u1");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(list);
    }

    [TestMethod]
    public async Task GetPersonalEntries_ForUserId_ReturnsOk()
    {
        IReadOnlyList<AchievementEntryDto> entries = [new AchievementEntryDto { Id = Guid.NewGuid() }];
        _sender.Send(new GetAchievementEntriesQuery(UserId: "u1"), Arg.Any<CancellationToken>()).Returns(entries);

        var result = await _sut.GetPersonalEntries("u1");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(entries);
    }

    [TestMethod]
    public async Task GetPersonalEntries_ForCurrentUser_ReturnsOk()
    {
        SetUser("testuser");
        IReadOnlyList<AchievementEntryDto> entries = [new AchievementEntryDto { Id = Guid.NewGuid() }];
        _sender.Send(new GetAchievementEntriesQuery(Username: "testuser"), Arg.Any<CancellationToken>()).Returns(entries);

        var result = await _sut.GetPersonalEntries();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(entries);
    }

    [TestMethod]
    public async Task GetPersonalUnseenEntriesForCurrentUser_ReturnsMarkedEntries()
    {
        SetUser("testuser");
        IReadOnlyList<AchievementEntryDto> entries = [new AchievementEntryDto { Id = Guid.NewGuid(), HasSeen = true }];
        _sender.Send(new MarkUnseenEntriesSeenCommand("testuser"), Arg.Any<CancellationToken>()).Returns(entries);

        var result = await _sut.GetPersonalUnseenEntriesForCurrentUser();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(entries);
    }

    [TestMethod]
    public async Task GetPersonalUnseenEntriesForCurrentUser_WhenUnauthorized_ReturnsUnauthorized()
    {
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await _sut.GetPersonalUnseenEntriesForCurrentUser();

        result.Should().BeOfType<UnauthorizedResult>();
        await _sender.DidNotReceive().Send(Arg.Any<MarkUnseenEntriesSeenCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task DeleteEntries_WithEmpty_ReturnsBadRequest()
    {
        var result = await _sut.DeleteEntries(new List<Guid>());

        result.Should().BeOfType<BadRequestResult>();
    }

    [TestMethod]
    public async Task DeleteEntries_WithIds_SendsCommand_ReturnsNoContent()
    {
        var ids = new List<Guid> { Guid.NewGuid() };

        var result = await _sut.DeleteEntries(ids);

        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(Arg.Is<DeleteAchievementEntriesCommand>(c => c.EntryIds.SequenceEqual(ids)), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetEarners_ReturnsOk()
    {
        var id = Guid.NewGuid();
        IReadOnlyList<AchievementEntryDto> entries = [new AchievementEntryDto { Id = Guid.NewGuid() }];
        _sender.Send(new GetAchievementEarnersQuery(id), Arg.Any<CancellationToken>()).Returns(entries);

        var result = await _sut.GetEarners(id);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(entries);
    }

    [TestMethod]
    public async Task GetRarity_ReturnsEarnerCount()
    {
        var id = Guid.NewGuid();
        IReadOnlyList<AchievementEntryDto> entries = [new AchievementEntryDto(), new AchievementEntryDto()];
        _sender.Send(new GetAchievementEarnersQuery(id), Arg.Any<CancellationToken>()).Returns(entries);

        var result = await _sut.GetRarity(id);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(new { Count = 2 });
    }

    [TestMethod]
    public async Task AwardToUser_WhenAlreadyAwarded_ReturnsConflict()
    {
        var id = Guid.NewGuid();
        _sender.Send(new AwardAchievementToUserCommand("u1", id), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.AwardToUser("u1", id);

        result.Should().BeOfType<ConflictResult>();
    }

    [TestMethod]
    public async Task AwardToUser_WhenSucceeds_ReturnsOkTrue()
    {
        var id = Guid.NewGuid();
        _sender.Send(new AwardAchievementToUserCommand("u1", id), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.AwardToUser("u1", id);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [TestMethod]
    public async Task AwardToAll_ReturnsCount()
    {
        var id = Guid.NewGuid();
        _sender.Send(new AwardAchievementToAllUsersCommand(id), Arg.Any<CancellationToken>()).Returns(3);

        var result = await _sut.AwardToAll(id);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(3);
    }

    [TestMethod]
    public async Task Create_WithValidImage_SendsCommandAndReturnsCreated()
    {
        var dto = new AchievementCreateDto
        {
            Name = "New",
            Description = "Desc",
            Image = new Base64FileUploadDto
            {
                FileName = "photo.png",
                ContentType = "image/png",
                Base64Content = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })
            }
        };

        var result = await _sut.Create(dto);

        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        await _sender.Received(1).Send(Arg.Is<CreateAchievementCommand>(c =>
            c.Achievement.Name == "New" && c.Achievement.ImageContentType != null), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Create_WithoutImage_ReturnsBadRequest()
    {
        var result = await _sut.Create(new AchievementCreateDto { Name = "New", Description = "Desc" });

        result.Should().BeOfType<BadRequestObjectResult>();
        await _sender.DidNotReceive().Send(Arg.Any<CreateAchievementCommand>(), Arg.Any<CancellationToken>());
    }
}
