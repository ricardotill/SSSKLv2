using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Application.Features.Announcements.Commands.CreateAnnouncement;
using SSSKL.Application.Features.Announcements.Commands.DeleteAnnouncement;
using SSSKL.Application.Features.Announcements.Commands.UpdateAnnouncement;
using SSSKL.Application.Features.Announcements.Queries.GetAllAnnouncements;
using SSSKL.Application.Features.Announcements.Queries.GetAnnouncementById;
using SSSKL.Application.Features.Announcements.Queries.GetAnnouncements;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Test.Features.Announcements;

[TestClass]
public class AnnouncementHandlerTests
{
    private IAnnouncementRepository _mockRepository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = Substitute.For<IAnnouncementRepository>();
    }

    #region Query Tests

    [TestMethod]
    public async Task GetAllAnnouncements_ShouldReturnAllAnnouncementsFromRepository()
    {
        // Arrange
        var announcements = new List<Announcement> { CreateAnnouncement("A"), CreateAnnouncement("B") };
        _mockRepository.GetAll().Returns(announcements);
        var sut = new GetAllAnnouncementsHandler(_mockRepository);

        // Act
        var result = await sut.Handle(new GetAllAnnouncementsQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(announcements);
    }

    [TestMethod]
    public async Task GetAllAnnouncements_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        _mockRepository.GetAll().Throws(new InvalidOperationException("Database error"));
        var sut = new GetAllAnnouncementsHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new GetAllAnnouncementsQuery(), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database error");
    }

    [TestMethod]
    public async Task GetAnnouncements_ReturnsPageAndTotalCount()
    {
        // Arrange
        var announcements = new List<Announcement> { CreateAnnouncement("A") };
        _mockRepository.GetAllPaged(5, 10).Returns(announcements);
        _mockRepository.GetCount().Returns(6);
        var sut = new GetAnnouncementsHandler(_mockRepository);

        // Act
        var result = await sut.Handle(new GetAnnouncementsQuery(5, 10), CancellationToken.None);

        // Assert
        result.Items.Should().BeEquivalentTo(announcements);
        result.TotalCount.Should().Be(6);
    }

    [TestMethod]
    public async Task GetAnnouncementById_WithValidId_ShouldReturnAnnouncement()
    {
        // Arrange
        var announcement = CreateAnnouncement("A");
        _mockRepository.GetById(announcement.Id).Returns(announcement);
        var sut = new GetAnnouncementByIdHandler(_mockRepository);

        // Act
        var result = await sut.Handle(new GetAnnouncementByIdQuery(announcement.Id), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(announcement);
    }

    [TestMethod]
    public async Task GetAnnouncementById_WhenRepositoryReturnsNull_ShouldThrowNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.GetById(id).Returns((Announcement?)null);
        var sut = new GetAnnouncementByIdHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new GetAnnouncementByIdQuery(id), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task GetAnnouncementById_WhenRepositoryThrowsNotFound_ShouldPropagateException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.GetById(id).Throws(new NotFoundException("Announcement not found"));
        var sut = new GetAnnouncementByIdHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new GetAnnouncementByIdQuery(id), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("Announcement not found");
    }

    #endregion

    #region Command Tests

    [TestMethod]
    public async Task CreateAnnouncement_ShouldMapDtoAndCallRepository()
    {
        // Arrange
        var dto = new AnnouncementCreateDto { Message = "Hello", Description = "World", Order = 2, IsScheduled = true, PlannedFrom = DateTime.UtcNow };
        var sut = new CreateAnnouncementHandler(_mockRepository);

        // Act
        var result = await sut.Handle(new CreateAnnouncementCommand(dto), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(new { dto.Message, dto.Description, dto.Order, dto.IsScheduled, dto.PlannedFrom });
        await _mockRepository.Received(1).Create(result);
    }

    [TestMethod]
    public async Task CreateAnnouncement_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        _mockRepository.Create(Arg.Any<Announcement>()).Throws(new InvalidOperationException("Database error"));
        var sut = new CreateAnnouncementHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new CreateAnnouncementCommand(new AnnouncementCreateDto { Message = "x" }), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database error");
    }

    [TestMethod]
    public async Task UpdateAnnouncement_ShouldApplyDtoAndCallRepository()
    {
        // Arrange
        var existing = CreateAnnouncement("Old");
        _mockRepository.GetById(existing.Id).Returns(existing);
        var dto = new AnnouncementUpdateDto { Message = "New", Description = "D", Order = 5, IsScheduled = false };
        var sut = new UpdateAnnouncementHandler(_mockRepository);

        // Act
        await sut.Handle(new UpdateAnnouncementCommand(existing.Id, dto), CancellationToken.None);

        // Assert
        existing.Message.Should().Be("New");
        existing.Description.Should().Be("D");
        existing.Order.Should().Be(5);
        await _mockRepository.Received(1).Update(existing);
    }

    [TestMethod]
    public async Task UpdateAnnouncement_WhenNotFound_ShouldThrowAndNotUpdate()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.GetById(id).Returns((Announcement?)null);
        var sut = new UpdateAnnouncementHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new UpdateAnnouncementCommand(id, new AnnouncementUpdateDto { Message = "x" }), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
        await _mockRepository.DidNotReceive().Update(Arg.Any<Announcement>());
    }

    [TestMethod]
    public async Task DeleteAnnouncement_WithValidId_ShouldCallRepositoryDelete()
    {
        // Arrange
        var existing = CreateAnnouncement("A");
        _mockRepository.GetById(existing.Id).Returns(existing);
        var sut = new DeleteAnnouncementHandler(_mockRepository);

        // Act
        await sut.Handle(new DeleteAnnouncementCommand(existing.Id), CancellationToken.None);

        // Assert
        await _mockRepository.Received(1).Delete(existing.Id);
    }

    [TestMethod]
    public async Task DeleteAnnouncement_WhenNotFound_ShouldThrowAndNotDelete()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.GetById(id).Returns((Announcement?)null);
        var sut = new DeleteAnnouncementHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new DeleteAnnouncementCommand(id), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
        await _mockRepository.DidNotReceive().Delete(Arg.Any<Guid>());
    }

    #endregion

    private static Announcement CreateAnnouncement(string message) => new()
    {
        Id = Guid.NewGuid(),
        Message = message,
        Description = $"Description for {message}",
        CreatedOn = DateTime.UtcNow
    };
}
