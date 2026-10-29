using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Application.Features.TopUps.Commands.CreateTopUp;
using SSSKL.Application.Features.TopUps.Commands.DeleteTopUp;
using SSSKL.Application.Features.TopUps.Queries.GetPersonalTopUps;
using SSSKL.Application.Features.TopUps.Queries.GetTopUpById;
using SSSKL.Application.Features.TopUps.Queries.GetTopUps;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Exceptions;
using SSSKL.Web.Controllers.v1;

namespace SSSKL.Web.Test.Controllers;

[TestClass]
public class TopUpControllerTests
{
    private ISender _sender = null!;
    private TopUpController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _sender = Substitute.For<ISender>();
        _sut = new TopUpController(_sender);
    }

    private void SetUser(string? name)
    {
        var claims = name is null ? Array.Empty<Claim>() : [new Claim(ClaimTypes.Name, name)];
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, name is null ? null : "TestAuth"))
            }
        };
    }

    [TestMethod]
    public async Task GetAll_ReturnsOkWithItems()
    {
        // Arrange
        SetUser("admin");
        var page = new PaginationObject<TopUpDto> { Items = [new TopUpDto { Id = Guid.NewGuid(), UserName = "u1", Saldo = 1.5m }], TotalCount = 1 };
        _sender.Send(new GetTopUpsQuery(0, 15), Arg.Any<CancellationToken>()).Returns(page);

        // Act
        var result = await _sut.GetAll();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
    }

    [TestMethod]
    public async Task GetPersonal_ReturnsOkWithItemsForCurrentUser()
    {
        // Arrange
        SetUser("user1");
        var page = new PaginationObject<TopUpDto> { Items = [new TopUpDto { Id = Guid.NewGuid(), UserName = "user1", Saldo = 3m }], TotalCount = 1 };
        _sender.Send(new GetPersonalTopUpsQuery("user1", 0, 15), Arg.Any<CancellationToken>()).Returns(page);

        // Act
        var result = await _sut.GetPersonal();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
    }

    [TestMethod]
    public async Task GetAll_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        SetUser(null);

        // Act
        var result = await _sut.GetAll();

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
        await _sender.DidNotReceive().Send(Arg.Any<GetTopUpsQuery>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetPersonal_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        SetUser(null);

        // Act
        var result = await _sut.GetPersonal();

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [TestMethod]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new TopUpDto { Id = id, UserName = "u", Saldo = 2m };
        _sender.Send(new GetTopUpByIdQuery(id), Arg.Any<CancellationToken>()).Returns(dto);

        // Act
        var result = await _sut.GetById(id);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(dto);
    }

    [TestMethod]
    public async Task GetById_WhenNotFound_PropagatesNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _sender.Send(new GetTopUpByIdQuery(id), Arg.Any<CancellationToken>()).Throws(new NotFoundException("TopUp not found"));

        // Act
        var act = () => _sut.GetById(id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Create_WithValidDto_ReturnsCreated_And_SendsCommand()
    {
        // Arrange
        var created = new TopUpDto { Id = Guid.NewGuid(), UserName = "u1", Saldo = 5m };
        _sender.Send(new CreateTopUpCommand("u1", 5m), Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _sut.Create(new TopUpCreateDto { UserName = "u1", Saldo = 5m });

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().BeSameAs(created);
        createdResult.RouteValues!["id"].Should().Be(created.Id);
    }

    [TestMethod]
    public async Task Create_WithNull_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.Create(null);

        // Assert
        result.Result.Should().BeOfType<BadRequestResult>();
    }

    [TestMethod]
    public async Task Create_WithInvalidDto_ReturnsBadRequest_And_DoesNotSendCommand()
    {
        // Arrange
        _sut.ModelState.AddModelError("Saldo", "Saldo must be greater than zero");

        // Act
        var result = await _sut.Create(new TopUpCreateDto { UserName = "u1", Saldo = 0m });

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        await _sender.DidNotReceive().Send(Arg.Any<CreateTopUpCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Create_WhenUserNotFound_PropagatesNotFoundException()
    {
        // Arrange
        _sender.Send(Arg.Any<CreateTopUpCommand>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("User not found"));

        // Act
        var act = () => _sut.Create(new TopUpCreateDto { UserName = "ghost", Saldo = 5m });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Delete_ReturnsNoContent_And_SendsCommand()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new DeleteTopUpCommand(id), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Delete_WhenNotFound_PropagatesNotFoundException()
    {
        // Arrange
        _sender.Send(Arg.Any<DeleteTopUpCommand>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("TopUp not found"));

        // Act
        var act = () => _sut.Delete(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
