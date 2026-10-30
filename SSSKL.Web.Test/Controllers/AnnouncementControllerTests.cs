using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Application.Features.Announcements.Commands.CreateAnnouncement;
using SSSKL.Application.Features.Announcements.Commands.DeleteAnnouncement;
using SSSKL.Application.Features.Announcements.Commands.UpdateAnnouncement;
using SSSKL.Application.Features.Announcements.Queries.GetAnnouncementById;
using SSSKL.Application.Features.Announcements.Queries.GetAnnouncements;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;
using SSSKL.Web.Controllers.v1;

namespace SSSKL.Web.Test.Controllers;

[TestClass]
public class AnnouncementControllerTests
{
    private ISender _sender = null!;
    private AnnouncementController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _sender = Substitute.For<ISender>();
        _sut = new AnnouncementController(_sender);
    }

    [TestMethod]
    public async Task GetAll_ReturnsOkWithItems()
    {
        // Arrange
        var page = new PaginationObject<Announcement> { Items = [new Announcement { Id = Guid.NewGuid(), Message = "M" }], TotalCount = 1 };
        _sender.Send(new GetAnnouncementsQuery(0, 15), Arg.Any<CancellationToken>()).Returns(page);

        // Act
        var result = await _sut.GetAll();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
    }

    [TestMethod]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        // Arrange
        var announcement = new Announcement { Id = Guid.NewGuid(), Message = "M" };
        _sender.Send(new GetAnnouncementByIdQuery(announcement.Id), Arg.Any<CancellationToken>()).Returns(announcement);

        // Act
        var result = await _sut.GetById(announcement.Id);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(announcement);
    }

    [TestMethod]
    public async Task GetById_WhenNotFound_PropagatesNotFoundException()
    {
        // Arrange
        _sender.Send(Arg.Any<GetAnnouncementByIdQuery>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("Announcement not found"));

        // Act
        var act = () => _sut.GetById(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Create_WithValidDto_ReturnsCreated()
    {
        // Arrange
        var dto = new AnnouncementCreateDto { Message = "Hello", Order = 1 };
        var created = new Announcement { Id = Guid.NewGuid(), Message = "Hello", Order = 1 };
        _sender.Send(Arg.Is<CreateAnnouncementCommand>(c => c.Announcement == dto), Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _sut.Create(dto);

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
    public async Task Create_WithInvalidDto_ReturnsBadRequest()
    {
        // Arrange
        _sut.ModelState.AddModelError("Message", "Required");

        // Act
        var result = await _sut.Create(new AnnouncementCreateDto());

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        await _sender.DidNotReceive().Send(Arg.Any<CreateAnnouncementCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Update_WhenFound_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new AnnouncementUpdateDto { Message = "Upd" };

        // Act
        var result = await _sut.Update(id, dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new UpdateAnnouncementCommand(id, dto), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Update_WhenNotFound_PropagatesNotFoundException()
    {
        // Arrange
        _sender.Send(Arg.Any<UpdateAnnouncementCommand>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("Announcement not found"));

        // Act
        var act = () => _sut.Update(Guid.NewGuid(), new AnnouncementUpdateDto { Message = "x" });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Update_WithInvalidDto_ReturnsBadRequest()
    {
        // Arrange
        _sut.ModelState.AddModelError("Message", "Required");

        // Act
        var result = await _sut.Update(Guid.NewGuid(), new AnnouncementUpdateDto());

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        await _sender.DidNotReceive().Send(Arg.Any<UpdateAnnouncementCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Delete_WhenFound_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new DeleteAnnouncementCommand(id), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Delete_WhenNotFound_PropagatesNotFoundException()
    {
        // Arrange
        _sender.Send(Arg.Any<DeleteAnnouncementCommand>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("Announcement not found"));

        // Act
        var act = () => _sut.Delete(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
