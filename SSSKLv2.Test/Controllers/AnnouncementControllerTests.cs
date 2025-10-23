using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SSSKLv2.Controllers.v1;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Test.Controllers;

[TestClass]
public class AnnouncementControllerTests
{
    private IAnnouncementService _mockService = null!;
    private AnnouncementController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _mockService = Substitute.For<IAnnouncementService>();
        _sut = new AnnouncementController(_mockService);
    }

    [TestMethod]
    public async Task GetAll_ReturnsOkWithItems()
    {
        // Arrange
        var items = new List<Announcement>
        {
            new Announcement { Id = Guid.NewGuid(), Message = "A1", CreatedOn = DateTime.Now },
            new Announcement { Id = Guid.NewGuid(), Message = "A2", CreatedOn = DateTime.Now }
        };
        _mockService.GetAllAnnouncements().Returns(items);

        // Act
        var result = await _sut.GetAll();

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeEquivalentTo(items);
    }

    [TestMethod]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var ann = new Announcement { Id = id, Message = "Found", CreatedOn = DateTime.Now };
        _mockService.GetAnnouncementById(id).Returns(Task.FromResult<Announcement?>(ann));

        // Act
        var result = await _sut.GetById(id);

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeEquivalentTo(ann);
    }

    [TestMethod]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockService.GetAnnouncementById(id).Returns(Task.FromException<Announcement?>(new NotFoundException("Announcement not found")));

        // Act
        var result = await _sut.GetById(id);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task Create_WithValidDto_ReturnsCreated()
    {
        // Arrange
        var dto = new AnnouncementCreateDto
        {
            Message = "Hello",
            IsScheduled = false
        };

        // Act
        var result = await _sut.Create(dto);

        // Assert
        var created = result.Result as CreatedAtActionResult;
        created.Should().NotBeNull();
        // Verify service was called
        await _mockService.Received(1).CreateAnnouncement(Arg.Any<Announcement>());
    }

    [TestMethod]
    public async Task Create_WithInvalidDto_ReturnsBadRequest()
    {
        // Arrange: scheduled but no PlannedFrom/PlannedTill -> validator should fail
        var dto = new AnnouncementCreateDto
        {
            Message = "Scheduled",
            IsScheduled = true
        };

        // Act
        var result = await _sut.Create(dto!);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        await _mockService.DidNotReceive().CreateAnnouncement(Arg.Any<Announcement>());
    }

    [TestMethod]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new AnnouncementUpdateDto { Message = "x", IsScheduled = false };
        _mockService.GetAnnouncementById(id).Returns(Task.FromException<Announcement?>(new NotFoundException("Announcement not found")));

        // Act
        var result = await _sut.Update(id, dto);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockService.GetAnnouncementById(id).Returns(Task.FromException<Announcement?>(new NotFoundException("Announcement not found")));

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}

