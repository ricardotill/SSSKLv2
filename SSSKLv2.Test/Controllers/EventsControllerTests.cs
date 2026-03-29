using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SSSKLv2.Controllers.v1;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Dto.Api;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Services.Interfaces;
using System.Security.Claims;

namespace SSSKLv2.Test.Controllers;

[TestClass]
public class EventsControllerTests
{
    private IEventService _mockService = null!;
    private ILogger<EventsController> _mockLogger = null!;
    private EventsController _sut = null!;
    private string _currentUserId = "test-user-id";

    [TestInitialize]
    public void Init()
    {
        _mockService = Substitute.For<IEventService>();
        _mockLogger = Substitute.For<ILogger<EventsController>>();
        _sut = new EventsController(_mockService, _mockLogger);

        // Mock User
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
    public async Task GetAll_ReturnsOkWithPagination()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new EventDto { Id = Guid.NewGuid(), Title = "Event 1" },
            new EventDto { Id = Guid.NewGuid(), Title = "Event 2" }
        };
        _mockService.GetAllEvents(0, 15, false, _currentUserId).Returns(events);
        _mockService.GetCount(false, _currentUserId).Returns(2);

        // Act
        var result = await _sut.GetAll();

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var paged = ok!.Value as PaginationObject<EventDto>;
        paged.Should().NotBeNull();
        paged!.Items.Should().BeEquivalentTo(events);
        paged.TotalCount.Should().Be(2);
    }

    [TestMethod]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var e = new EventDto { Id = id, Title = "Found Event" };
        _mockService.GetEventById(id, _currentUserId).Returns(e);

        // Act
        var result = await _sut.GetById(id);

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeEquivalentTo(e);
    }

    [TestMethod]
    public async Task Create_ReturnsCreated()
    {
        // Arrange
        var dto = new EventCreateDto { Title = "New Event" };
        var eventId = Guid.NewGuid();
        var createdEvent = new EventDto { Id = eventId, Title = "New Event" };

        _mockService.CreateEvent(dto, _currentUserId).Returns(eventId);
        _mockService.GetEventById(eventId, _currentUserId).Returns(createdEvent);

        // Act
        var result = await _sut.Create(dto, null);

        // Assert
        var created = result.Result as CreatedAtActionResult;
        created.Should().NotBeNull();
        created!.ActionName.Should().Be(nameof(EventsController.GetById));
        created.Value.Should().BeEquivalentTo(createdEvent);
    }

    [TestMethod]
    public async Task Respond_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new EventResponseDto { Status = EventResponseStatus.Accepted };

        // Act
        var result = await _sut.Respond(id, dto);

        // Assert
        result.Should().BeOfType<OkResult>();
        await _mockService.Received(1).RespondToEvent(id, _currentUserId, EventResponseStatus.Accepted);
    }

    [TestMethod]
    public async Task Delete_WhenCreator_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _mockService.Received(1).DeleteEvent(id, _currentUserId, false);
    }
}
