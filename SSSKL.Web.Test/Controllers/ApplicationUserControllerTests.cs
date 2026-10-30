using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Exceptions;
using SSSKL.Web.Controllers.v1;

namespace SSSKL.Web.Test.Controllers;

[TestClass]
public class ApplicationUserControllerTests
{
    private ISender _sender = null!;
    private ApplicationUserController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _sender = Substitute.For<ISender>();
        _sut = new ApplicationUserController(_sender, Substitute.For<ILogger<ApplicationUserController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private void SetUser(string? id = null, string? username = null, bool isAdmin = false)
    {
        var claims = new List<Claim>();
        if (id != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, id));
        if (username != null) claims.Add(new Claim(ClaimTypes.Name, username));
        if (isAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static ApplicationUserDetailedDto Detailed(string id, string username) => new() { Id = id, UserName = username };

    #region Queries

    [TestMethod]
    public async Task GetAll_ReturnsOkWithItems()
    {
        var page = new PaginationObject<ApplicationUserDto> { Items = [new ApplicationUserDto { Id = "u1" }], TotalCount = 1 };
        _sender.Send(new GetUsersQuery(0, 15), Arg.Any<CancellationToken>()).Returns(page);

        var result = await _sut.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
    }

    [TestMethod]
    public async Task GetAllAdmin_ReturnsOkWithItems()
    {
        var page = new PaginationObject<ApplicationUserDto> { Items = [new ApplicationUserDto { Id = "u1" }], TotalCount = 1 };
        _sender.Send(new GetUsersQuery(0, 15, ForAdmin: true), Arg.Any<CancellationToken>()).Returns(page);

        var result = await _sut.GetAllAdmin();

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
    }

    [TestMethod]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var dto = Detailed("user1", "u1");
        _sender.Send(new GetUserDetailsQuery(UserReference.ById("user1")), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.GetById("user1");

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(dto);
    }

    [TestMethod]
    public async Task GetById_WhenNotFound_PropagatesNotFoundException()
    {
        _sender.Send(Arg.Any<GetUserDetailsQuery>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("ApplicationUser not found"));

        var act = () => _sut.GetById("missing");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task GetByUsername_WhenFound_ReturnsOk()
    {
        var dto = Detailed("u2", "john");
        _sender.Send(new GetUserDetailsQuery(UserReference.ByUsername("john")), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.GetByUsername("john");

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(dto);
    }

    [TestMethod]
    public async Task GetProfileById_WhenFound_ReturnsOk()
    {
        var dto = new ApplicationUserDto { Id = "user-profile-1" };
        _sender.Send(new GetUserProfileQuery("user-profile-1"), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.GetProfileById("user-profile-1");

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(dto);
    }

    [TestMethod]
    public async Task GetCurrent_WhenAuthenticated_ReturnsOkWithUser()
    {
        SetUser(username: "me");
        var dto = Detailed("me-id", "me");
        _sender.Send(new GetUserDetailsQuery(UserReference.ByUsername("me")), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.GetCurrent();

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(dto);
    }

    [TestMethod]
    public async Task GetCurrent_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var result = await _sut.GetCurrent();

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [TestMethod]
    public async Task DownloadPersonalData_WhenSuccess_ReturnsJsonFile()
    {
        SetUser(id: "u-personal");
        IReadOnlyDictionary<string, string> data = new Dictionary<string, string> { ["Name"] = "Personal" };
        _sender.Send(new GetPersonalDataQuery("u-personal"), Arg.Any<CancellationToken>()).Returns(data);

        var result = await _sut.DownloadPersonalData();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("PersonalData.json");
        file.ContentType.Should().Be("application/json");
        JsonSerializer.Deserialize<Dictionary<string, string>>(file.FileContents).Should().ContainKey("Name");
    }

    [TestMethod]
    public async Task DownloadPersonalData_WhenUserIdMissing_ReturnsNotFound()
    {
        var result = await _sut.DownloadPersonalData();

        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Update

    [TestMethod]
    public async Task Update_AsAdmin_SendsCommandWithoutUserName()
    {
        SetUser(username: "admin", isAdmin: true);
        var dto = new ApplicationUserUpdateDto { Id = "user1", UserName = "renamed", Name = "New" };
        var updated = Detailed("user1", "u1");
        _sender.Send(Arg.Any<UpdateUserCommand>(), Arg.Any<CancellationToken>()).Returns(updated);

        var result = await _sut.Update("user1", dto);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(updated);
        await _sender.Received(1).Send(Arg.Is<UpdateUserCommand>(c =>
            c.User == UserReference.ById("user1") && c.Changes.UserName == null && c.IsAdmin && c.RequesterUsername == "admin"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Update_WhenDtoNull_ReturnsBadRequest()
    {
        var result = await _sut.Update("user1", null);

        result.Should().BeOfType<BadRequestResult>();
    }

    [TestMethod]
    public async Task Update_WhenIdMismatch_ReturnsBadRequest()
    {
        var result = await _sut.Update("user1", new ApplicationUserUpdateDto { Id = "other" });

        result.Should().BeOfType<BadRequestObjectResult>();
        await _sender.DidNotReceive().Send(Arg.Any<UpdateUserCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Update_WhenHandlerThrowsInvalidOperation_ReturnsBadRequestWithError()
    {
        SetUser(username: "admin", isAdmin: true);
        _sender.Send(Arg.Any<UpdateUserCommand>(), Arg.Any<CancellationToken>()).Throws(new InvalidOperationException("Email taken"));

        var result = await _sut.Update("user1", new ApplicationUserUpdateDto());

        result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().BeEquivalentTo(new { error = "Email taken" });
    }

    [TestMethod]
    public async Task Update_WhenNotOwnerAndNotAdmin_PropagatesUnauthorizedAccess()
    {
        SetUser(username: "someone");
        _sender.Send(Arg.Any<UpdateUserCommand>(), Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        var act = () => _sut.Update("user1", new ApplicationUserUpdateDto());

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task UpdateMe_SendsOnlySelfEditableFields()
    {
        SetUser(username: "me");
        var updated = Detailed("me-id", "me");
        _sender.Send(Arg.Any<UpdateUserCommand>(), Arg.Any<CancellationToken>()).Returns(updated);

        var result = await _sut.UpdateMe(new ApplicationUserSelfUpdateDto { Name = "N", Surname = "S", PhoneNumber = "1", Description = "D" });

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(updated);
        await _sender.Received(1).Send(Arg.Is<UpdateUserCommand>(c =>
            c.User == UserReference.ByUsername("me") && c.RequesterUsername == "me" && !c.IsAdmin &&
            c.Changes.Name == "N" && c.Changes.Surname == "S" && c.Changes.PhoneNumber == "1" && c.Changes.Description == "D" &&
            c.Changes.Roles == null && c.Changes.Password == null && c.Changes.Email == null),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpdateMe_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var result = await _sut.UpdateMe(new ApplicationUserSelfUpdateDto());

        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region Delete

    [TestMethod]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var result = await _sut.Delete("user1");

        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new DeleteUserCommand(UserReference.ById("user1")), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Delete_WhenHandlerThrowsInvalidOperation_ReturnsBadRequestWithError()
    {
        _sender.Send(Arg.Any<DeleteUserCommand>(), Arg.Any<CancellationToken>()).Throws(new InvalidOperationException("Cannot delete"));

        var result = await _sut.Delete("user1");

        result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().BeEquivalentTo(new { error = "Cannot delete" });
    }

    [TestMethod]
    public async Task Delete_WhenUserNotFound_PropagatesNotFoundException()
    {
        _sender.Send(Arg.Any<DeleteUserCommand>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("ApplicationUser not found"));

        var act = () => _sut.Delete("missing");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task DeleteMe_WhenAuthenticated_ReturnsNoContent()
    {
        SetUser(username: "me");

        var result = await _sut.DeleteMe();

        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new DeleteUserCommand(UserReference.ByUsername("me")), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task DeleteMe_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var result = await _sut.DeleteMe();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region Profile picture

    [TestMethod]
    public async Task UploadProfilePicture_WithValidFile_ReturnsOk()
    {
        SetUser(username: "me");
        var file = new Base64FileUploadDto { FileName = "p.png", ContentType = "image/png", Base64Content = Convert.ToBase64String(new byte[] { 1, 2, 3 }) };

        var result = await _sut.UploadProfilePicture(file);

        result.Should().BeOfType<OkResult>();
        await _sender.Received(1).Send(Arg.Is<UpdateProfilePictureCommand>(c => c.User == UserReference.ByUsername("me")), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UploadProfilePicture_WhenFileMissing_ReturnsBadRequest()
    {
        SetUser(username: "me");

        var result = await _sut.UploadProfilePicture(null);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task UploadProfilePicture_WhenImageInvalid_ReturnsBadRequest()
    {
        SetUser(username: "me");
        _sender.Send(Arg.Any<UpdateProfilePictureCommand>(), Arg.Any<CancellationToken>()).Throws(new UnknownImageFormatException("bad"));
        var file = new Base64FileUploadDto { FileName = "p.png", ContentType = "image/png", Base64Content = Convert.ToBase64String(new byte[] { 1, 2, 3 }) };

        var result = await _sut.UploadProfilePicture(file);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task UploadProfilePicture_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var result = await _sut.UploadProfilePicture(new Base64FileUploadDto());

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [TestMethod]
    public async Task DeleteProfilePicture_AsSelf_ReturnsNoContent()
    {
        SetUser(username: "me");

        var result = await _sut.DeleteProfilePicture();

        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new DeleteProfilePictureCommand(UserReference.ByUsername("me")), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task DeleteUserProfilePicture_AsAdmin_ReturnsNoContent()
    {
        var result = await _sut.DeleteUserProfilePicture("user1");

        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new DeleteProfilePictureCommand(UserReference.ById("user1")), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Stats

    [TestMethod]
    public async Task GetStats_SendsQueryWithRequester()
    {
        SetUser(id: "user1");
        var stats = new UserStatDto { UserId = "user1" };
        _sender.Send(new GetUserStatsQuery("user1", "user1", false), Arg.Any<CancellationToken>()).Returns(stats);

        var result = await _sut.GetStats("user1");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(stats);
    }

    [TestMethod]
    public async Task RecalculateStats_WhenAllowed_ReturnsOk()
    {
        SetUser(id: "admin", isAdmin: true);
        var stats = new UserStatDto { UserId = "target" };
        _sender.Send(new RecalculateUserStatsCommand("target", "admin", true), Arg.Any<CancellationToken>())
            .Returns(new RecalculateUserStatsResult(stats, null));

        var result = await _sut.RecalculateStats("target");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(stats);
    }

    [TestMethod]
    public async Task RecalculateStats_WhenRateLimited_Returns429()
    {
        SetUser(id: "user1");
        var next = DateTime.UtcNow.AddDays(3);
        _sender.Send(Arg.Any<RecalculateUserStatsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RecalculateUserStatsResult(null, next));

        var result = await _sut.RecalculateStats("user1");

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        objectResult.Value.Should().BeEquivalentTo(new { nextAllowedAt = next }, o => o.ExcludingMissingMembers());
    }

    [TestMethod]
    public async Task RecalculateAllStats_WhenStarted_ReturnsAccepted()
    {
        SetUser(id: "admin", isAdmin: true);
        var job = new RecalculationJobDto { Id = Guid.NewGuid() };
        _sender.Send(new StartStatsRecalculationCommand("admin"), Arg.Any<CancellationToken>()).Returns(new StartStatsRecalculationResult(job, true));

        var result = await _sut.RecalculateAllStats();

        result.Should().BeOfType<AcceptedAtActionResult>().Which.Value.Should().BeSameAs(job);
    }

    [TestMethod]
    public async Task RecalculateAllStats_WhenAlreadyRunning_ReturnsConflict()
    {
        SetUser(id: "admin", isAdmin: true);
        var job = new RecalculationJobDto { Id = Guid.NewGuid() };
        _sender.Send(Arg.Any<StartStatsRecalculationCommand>(), Arg.Any<CancellationToken>()).Returns(new StartStatsRecalculationResult(job, false));

        var result = await _sut.RecalculateAllStats();

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [TestMethod]
    public async Task GetLatestRecalculateAllStatsStatus_WhenNone_ReturnsNotFound()
    {
        _sender.Send(new GetStatsRecalculationStatusQuery(), Arg.Any<CancellationToken>()).Returns((RecalculationJobDto?)null);

        var result = await _sut.GetLatestRecalculateAllStatsStatus();

        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task GetRecalculateAllStatsStatus_WhenFound_ReturnsOk()
    {
        var job = new RecalculationJobDto { Id = Guid.NewGuid() };
        _sender.Send(new GetStatsRecalculationStatusQuery(job.Id), Arg.Any<CancellationToken>()).Returns(job);

        var result = await _sut.GetRecalculateAllStatsStatus(job.Id);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(job);
    }

    #endregion
}
