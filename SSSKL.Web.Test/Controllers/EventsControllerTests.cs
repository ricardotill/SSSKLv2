using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Application.Features.Events.Commands.CreateEvent;
using SSSKL.Application.Features.Events.Commands.DeleteEvent;
using SSSKL.Application.Features.Events.Commands.RespondToEvent;
using SSSKL.Application.Features.Events.Commands.UpdateEvent;
using SSSKL.Application.Features.Events.Commands.UpdateEventImage;
using SSSKL.Application.Features.Events.Queries.GetEventById;
using SSSKL.Application.Features.Events.Queries.GetEvents;
using SSSKL.Contracts.Dto.Api;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;
using SSSKL.Web.Controllers.v1;

namespace SSSKL.Web.Test.Controllers;

[TestClass]
public class EventsControllerTests
{
    private ISender _sender = null!;
    private EventsController _sut = null!;
    private readonly string _currentUserId = "test-user-id";

    [TestInitialize]
    public void Init()
    {
        _sender = Substitute.For<ISender>();
        _sut = new EventsController(_sender, Substitute.For<ILogger<EventsController>>());

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _currentUserId),
            new Claim(ClaimTypes.Name, "test@example.com")
        }, "TestAuthentication"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [TestMethod]
    public async Task GetAll_ReturnsOkWithPaginationForCurrentUser()
    {
        var page = new PaginationObject<EventDto> { Items = [new EventDto { Id = Guid.NewGuid(), Title = "Event 1" }], TotalCount = 1 };
        _sender.Send(new GetEventsQuery(0, 15, false, _currentUserId, null), Arg.Any<CancellationToken>()).Returns(page);

        var result = await _sut.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
    }

    [TestMethod]
    public async Task GetPublic_ReturnsOkWithPaginationWithoutUser()
    {
        var page = new PaginationObject<EventDto> { Items = [new EventDto { Id = Guid.NewGuid(), Title = "Public Event" }], TotalCount = 1 };
        _sender.Send(new GetEventsQuery(0, 15, false, null, null), Arg.Any<CancellationToken>()).Returns(page);

        var result = await _sut.GetPublic();

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
    }

    [TestMethod]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var e = new EventDto { Id = id, Title = "Found Event" };
        _sender.Send(new GetEventByIdQuery(id, _currentUserId), Arg.Any<CancellationToken>()).Returns(e);

        var result = await _sut.GetById(id);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(e);
    }

    [TestMethod]
    public async Task GetById_WhenNotFound_PropagatesNotFoundException()
    {
        var id = Guid.NewGuid();
        _sender.Send(new GetEventByIdQuery(id, _currentUserId), Arg.Any<CancellationToken>()).Throws(new NotFoundException("Not found"));

        var act = () => _sut.GetById(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Create_ReturnsCreated()
    {
        var dto = new EventCreateDto { Title = "New Event" };
        var createdEvent = new EventDto { Id = Guid.NewGuid(), Title = "New Event" };
        _sender.Send(new CreateEventCommand(dto, _currentUserId), Arg.Any<CancellationToken>()).Returns(createdEvent);

        var result = await _sut.Create(dto, null);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(EventsController.GetById));
        created.Value.Should().BeSameAs(createdEvent);
    }

    [TestMethod]
    public async Task Create_WithImage_SetsPropsAndReturnsCreated()
    {
        var dto = new EventCreateDto { Title = "Event with image" };
        var image = Substitute.For<IFormFile>();
        image.ContentType.Returns("image/png");
        image.OpenReadStream().Returns(new MemoryStream());
        _sender.Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>()).Returns(new EventDto { Id = Guid.NewGuid() });

        var result = await _sut.Create(dto, image);

        dto.ImageContentType.Should().NotBeNull();
        dto.ImageContent.Should().NotBeNull();
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [TestMethod]
    public async Task Create_WithUnsupportedImage_ReturnsBadRequest()
    {
        var image = Substitute.For<IFormFile>();
        image.ContentType.Returns("application/pdf");
        image.FileName.Returns("doc.pdf");

        var result = await _sut.Create(new EventCreateDto { Title = "x" }, image);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        await _sender.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpdateImage_WhenValidImage_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var image = new Base64FileUploadDto
        {
            FileName = "photo.png",
            ContentType = "image/png",
            Base64Content = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })
        };

        var result = await _sut.UpdateImage(id, image);

        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(Arg.Is<UpdateEventImageCommand>(c =>
            c.Id == id && c.UserId == _currentUserId && !c.IsAdmin && c.ContentType == "image/png"), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpdateImage_WhenHandlerRejectsImage_ReturnsBadRequest()
    {
        var image = new Base64FileUploadDto
        {
            FileName = "photo.png",
            ContentType = "image/png",
            Base64Content = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })
        };
        _sender.Send(Arg.Any<UpdateEventImageCommand>(), Arg.Any<CancellationToken>()).Throws(new ArgumentException("Unsupported"));

        var result = await _sut.UpdateImage(Guid.NewGuid(), image);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task Update_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var dto = new EventCreateDto { Title = "Updated" };

        var result = await _sut.Update(id, dto);

        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new UpdateEventCommand(id, dto, _currentUserId, false), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Update_WhenUnauthorized_PropagatesUnauthorizedAccess()
    {
        _sender.Send(Arg.Any<UpdateEventCommand>(), Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        var act = () => _sut.Update(Guid.NewGuid(), new EventCreateDto());

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task Delete_WhenCreator_ReturnsNoContent()
    {
        var id = Guid.NewGuid();

        var result = await _sut.Delete(id);

        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new DeleteEventCommand(id, _currentUserId, false), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Delete_WhenNotFound_PropagatesNotFoundException()
    {
        _sender.Send(Arg.Any<DeleteEventCommand>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("Not found"));

        var act = () => _sut.Delete(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Respond_ReturnsOk()
    {
        var id = Guid.NewGuid();

        var result = await _sut.Respond(id, new EventResponseDto { Status = EventResponseStatus.Accepted });

        result.Should().BeOfType<OkResult>();
        await _sender.Received(1).Send(new RespondToEventCommand(id, _currentUserId, EventResponseStatus.Accepted), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Respond_WhenUserIdMissing_ReturnsUnauthorized()
    {
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await _sut.Respond(Guid.NewGuid(), new EventResponseDto());

        result.Should().BeOfType<UnauthorizedResult>();
    }
}
