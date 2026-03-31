using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SSSKLv2.Agents;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Dto.Api;
using SSSKLv2.Services;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Test.Util;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.Constants;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using SSSKLv2.Data.DAL.Exceptions;
using System.IO;
using System.Net.Mime;

namespace SSSKLv2.Test.Services;

[TestClass]
public class EventServiceTests : RepositoryTest
{
    private EventService _sut = null!;
    private IEventRepository _eventRepository = null!;
    private IBlobStorageAgent _blobStorageAgent = null!;
    private IApplicationUserService _applicationUserService = null!;
    private IEventNotifier _eventNotifier = null!;
    private ApplicationDbContext _dbContext = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContext = new ApplicationDbContext(GetOptions());
        
        _eventRepository = Substitute.For<IEventRepository>();
        _blobStorageAgent = Substitute.For<IBlobStorageAgent>();
        _applicationUserService = Substitute.For<IApplicationUserService>();
        _eventNotifier = Substitute.For<IEventNotifier>();

        _sut = new EventService(_eventRepository, _blobStorageAgent, _applicationUserService, _dbContext, _eventNotifier);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task GetAllEvents_AsAdmin_ShouldCallRepositoryWithIsAdminTrue()
    {
        // Arrange
        var userId = "admin-id";
        _applicationUserService.GetUserRoles(userId).Returns(new List<string> { Roles.Admin });
        _eventRepository.GetAll(0, 15, false, Arg.Any<IList<string>>(), true, null)
            .Returns(new List<Event>());

        // Act
        var result = await _sut.GetAllEvents(0, 15, false, userId);

        // Assert
        result.Should().NotBeNull();
        await _eventRepository.Received(1).GetAll(0, 15, false, Arg.Is<IList<string>>(r => r.Contains(Roles.Admin)), true, null);
    }

    [TestMethod]
    public async Task GetCount_AsUser_ShouldCallRepositoryWithCorrectRoles()
    {
        // Arrange
        var userId = "user-id";
        var roles = new List<string> { "UserRole" };
        _applicationUserService.GetUserRoles(userId).Returns(roles);
        _eventRepository.GetCount(true, Arg.Any<IList<string>>(), false, null).Returns(5);

        // Act
        var result = await _sut.GetCount(true, userId);

        // Assert
        result.Should().Be(5);
        await _eventRepository.Received(1).GetCount(true, Arg.Is<IList<string>>(r => r.SequenceEqual(roles)), false, null);
    }

    [TestMethod]
    public async Task GetEventById_ExistentId_ShouldReturnDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var e = new Event { Id = id, Title = "Test Event", Creator = new ApplicationUser { Name = "Creator" } };
        _eventRepository.GetById(id).Returns(e);

        // Act
        var result = await _sut.GetEventById(id);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Event");
    }

    [TestMethod]
    public async Task GetEventById_NonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _eventRepository.GetById(id).Returns((Event)null!);

        // Act & Assert
        await _sut.Invoking(s => s.GetEventById(id)).Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task CreateEvent_ValidDto_ShouldCallAddAndNotify()
    {
        // Arrange
        var dto = new EventCreateDto { Title = "New Event", StartDateTime = DateTime.Now, EndDateTime = DateTime.Now.AddHours(1) };
        var creatorId = "creator-id";
        
        // Mock the repository to set an ID on the event object
        _eventRepository.Add(Arg.Do<Event>(e => e.Id = Guid.NewGuid())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateEvent(dto, creatorId);

        // Assert
        result.Should().NotBeEmpty();
        await _eventRepository.Received(1).Add(Arg.Is<Event>(e => e.Title == dto.Title && e.CreatorId == creatorId));
        await _eventNotifier.Received(1).NotifyEventChangedAsync();
    }

    [TestMethod]
    public async Task CreateEvent_WithRoles_ShouldFetchRolesFromDb()
    {
        // Arrange
        var roleName = "Participant";
        _dbContext.Roles.Add(new Microsoft.AspNetCore.Identity.IdentityRole(roleName));
        await _dbContext.SaveChangesAsync();

        var dto = new EventCreateDto 
        { 
            Title = "Role Event", 
            RequiredRoles = new List<string> { roleName } 
        };

        // Act
        await _sut.CreateEvent(dto, "creator-id");

        // Assert
        await _eventRepository.Received(1).Add(Arg.Is<Event>(e => e.RequiredRoles.Any(r => r.Name == roleName)));
    }

    [TestMethod]
    public async Task UpdateEvent_AsCreator_ShouldUpdateAndNotify()
    {
        // Arrange
        var id = Guid.NewGuid();
        var creatorId = "creator-id";
        var e = new Event { Id = id, CreatorId = creatorId, Title = "Old Title" };
        _eventRepository.GetById(id).Returns(e);

        var dto = new EventCreateDto { Title = "New Title" };

        // Act
        await _sut.UpdateEvent(id, dto, creatorId, false);

        // Assert
        e.Title.Should().Be("New Title");
        await _eventRepository.Received(1).Update(e);
        await _eventNotifier.Received(1).NotifyEventChangedAsync();
    }

    [TestMethod]
    public async Task UpdateEvent_AsNonCreatorNonAdmin_ShouldThrowUnauthorized()
    {
        // Arrange
        var id = Guid.NewGuid();
        var e = new Event { Id = id, CreatorId = "creator-id" };
        _eventRepository.GetById(id).Returns(e);

        var dto = new EventCreateDto { Title = "Illegally Updated" };

        // Act & Assert
        await _sut.Invoking(s => s.UpdateEvent(id, dto, "hacker-id", false)).Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task DeleteEvent_AsAdmin_ShouldDeleteAndNotify()
    {
        // Arrange
        var id = Guid.NewGuid();
        var e = new Event { Id = id, CreatorId = "creator-id" };
        _eventRepository.GetById(id).Returns(e);

        // Act
        await _sut.DeleteEvent(id, "admin-id", true);

        // Assert
        await _eventRepository.Received(1).Delete(id);
        await _eventNotifier.Received(1).NotifyEventChangedAsync();
    }

    [TestMethod]
    public async Task RespondToEvent_NoExistingResponse_ShouldAddResponse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = "user-id";
        var e = new Event { Id = id };
        _eventRepository.GetById(id).Returns(e);
        _eventRepository.GetResponse(id, userId).Returns((EventResponse)null!);

        // Act
        await _sut.RespondToEvent(id, userId, EventResponseStatus.Accepted);

        // Assert
        await _eventRepository.Received(1).AddResponse(Arg.Is<EventResponse>(r => r.EventId == id && r.UserId == userId && r.Status == EventResponseStatus.Accepted));
    }

    [TestMethod]
    public async Task RespondToEvent_ExistingResponse_ShouldUpdateResponse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = "user-id";
        var e = new Event { Id = id };
        var existingResponse = new EventResponse { EventId = id, UserId = userId, Status = EventResponseStatus.Declined };
        _eventRepository.GetById(id).Returns(e);
        _eventRepository.GetResponse(id, userId).Returns(existingResponse);

        // Act
        await _sut.RespondToEvent(id, userId, EventResponseStatus.Accepted);

        // Assert
        existingResponse.Status.Should().Be(EventResponseStatus.Accepted);
        await _eventRepository.Received(1).UpdateResponse(existingResponse);
    }

    [TestMethod]
    public async Task CreateEvent_WithImage_ShouldUploadAndSetImage()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0, 1, 2 });
        var dto = new EventCreateDto 
        { 
            Title = "Image Event", 
            ImageContent = stream,
            ImageContentType = new ContentType("image/png")
        };
        
        _blobStorageAgent.UploadFileToBlobAsync(Arg.Any<string>(), "image/png", Arg.Any<Stream>())
            .Returns(new BlobStorageItem { Id = Guid.NewGuid(), FileName = "test.png", Uri = "http://test.com/test.png", ContentType = "image/png" });

        // Act
        await _sut.CreateEvent(dto, "creator-id");

        // Assert
        await _eventRepository.Received(1).Add(Arg.Is<Event>(e => e.Image != null));
        await _blobStorageAgent.Received(1).UploadFileToBlobAsync(Arg.Any<string>(), "image/png", Arg.Any<Stream>());
    }

    [TestMethod]
    public async Task RespondToEvent_NotAuthorizedRole_ShouldThrowUnauthorized()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = "user-id";
        var role = new Microsoft.AspNetCore.Identity.IdentityRole("RequiredRole");
        var e = new Event { Id = id, RequiredRoles = new List<Microsoft.AspNetCore.Identity.IdentityRole> { role } };
        
        _eventRepository.GetById(id).Returns(e);
        _applicationUserService.GetUserRoles(userId).Returns(new List<string> { "OtherRole" });

        // Act & Assert
        await _sut.Invoking(s => s.RespondToEvent(id, userId, EventResponseStatus.Accepted)).Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
