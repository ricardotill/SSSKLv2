using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SSSKLv2.Test.Services;

[TestClass]
public class AnnouncementServiceTests
{
    private IAnnouncementRepository _mockRepository = null!;
    private AnnouncementService _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = Substitute.For<IAnnouncementRepository>();
        _sut = new AnnouncementService(_mockRepository);
    }

    #region GetAllAnnouncements Tests

    [TestMethod]
    public async Task GetAllAnnouncements_ShouldReturnAllAnnouncementsFromRepository()
    {
        // Arrange
        var expectedAnnouncements = new List<Announcement>
        {
            CreateAnnouncement("Test Announcement 1"),
            CreateAnnouncement("Test Announcement 2")
        };

        _mockRepository.GetAll().Returns(expectedAnnouncements);

        // Act
        var result = await _sut.GetAllAnnouncements();

        // Assert
        result.Should().BeEquivalentTo(expectedAnnouncements);
        await _mockRepository.Received(1).GetAll();
    }

    [TestMethod]
    public async Task GetAllAnnouncements_WhenRepositoryReturnsEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        _mockRepository.GetAll().Returns(new List<Announcement>());

        // Act
        var result = await _sut.GetAllAnnouncements();

        // Assert
        result.Should().BeEmpty();
        await _mockRepository.Received(1).GetAll();
    }

    [TestMethod]
    public async Task GetAllAnnouncements_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        _mockRepository.GetAll().Returns(Task.FromException<IList<Announcement>>(expectedException));

        // Act
        Func<Task> action = async () => await _sut.GetAllAnnouncements();

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Test exception");
        await _mockRepository.Received(1).GetAll();
    }

    #endregion

    #region GetAllAnnouncementsQueryable Tests

    [TestMethod]
    public void GetAllAnnouncementsQueryable_ShouldReturnQueryableFromRepository()
    {
        // Arrange
        var announcements = new List<Announcement>
        {
            CreateAnnouncement("Test Announcement 1"),
            CreateAnnouncement("Test Announcement 2")
        }.AsQueryable();

        _mockRepository.GetAllQueryable(Arg.Any<ApplicationDbContext>()).Returns(announcements);

        // Act
        var result = _sut.GetAllAnnouncementsQueryable(default!);

        // Assert
        result.Should().BeEquivalentTo(announcements);
        _mockRepository.Received(1).GetAllQueryable(default!);
    }

    [TestMethod]
    public void GetAllAnnouncementsQueryable_WhenRepositoryReturnsEmptyQueryable_ShouldReturnEmptyQueryable()
    {
        // Arrange
        var emptyQueryable = new List<Announcement>().AsQueryable();
        _mockRepository.GetAllQueryable(Arg.Any<ApplicationDbContext>()).Returns(emptyQueryable);

        // Act
        var result = _sut.GetAllAnnouncementsQueryable(default!);

        // Assert
        result.Should().BeEmpty();
        _mockRepository.Received(1).GetAllQueryable(default!);
    }

    [TestMethod]
    public void GetAllAnnouncementsQueryable_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        _mockRepository.GetAllQueryable(Arg.Any<ApplicationDbContext>()).Returns(_ => { throw expectedException; });

        // Act
        Action action = () => _sut.GetAllAnnouncementsQueryable(default!);

        // Assert
        action.Should().Throw<InvalidOperationException>().WithMessage("Test exception");
        _mockRepository.Received(1).GetAllQueryable(default!);
    }

    #endregion

    #region GetAnnouncementById Tests

    [TestMethod]
    public async Task GetAnnouncementById_WithValidId_ShouldReturnAnnouncementFromRepository()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expectedAnnouncement = CreateAnnouncement("Test Announcement", id);
        _mockRepository.GetById(id).Returns(expectedAnnouncement);

        // Act
        var result = await _sut.GetAnnouncementById(id);

        // Assert
        result.Should().BeEquivalentTo(expectedAnnouncement);
        await _mockRepository.Received(1).GetById(id);
    }

    [TestMethod]
    public async Task GetAnnouncementById_WithNonExistentId_ShouldPropagateNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.GetById(id).Returns(Task.FromException<Announcement>(new NotFoundException("Announcement not found")));

        // Act
        Func<Task> action = async () => await _sut.GetAnnouncementById(id);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("Announcement not found");
        await _mockRepository.Received(1).GetById(id);
    }

    [TestMethod]
    public async Task GetAnnouncementById_WithEmptyGuid_ShouldPropagateException()
    {
        // Arrange
        var id = Guid.Empty;
        _mockRepository.GetById(id).Returns(Task.FromException<Announcement>(new ArgumentException("Invalid ID")));

        // Act
        Func<Task> action = async () => await _sut.GetAnnouncementById(id);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid ID");
        await _mockRepository.Received(1).GetById(id);
    }

    #endregion

    #region CreateAnnouncement Tests

    [TestMethod]
    public async Task CreateAnnouncement_WithValidAnnouncement_ShouldCallRepositoryCreate()
    {
        // Arrange
        var announcement = CreateAnnouncement("New Announcement");

        // Act
        await _sut.CreateAnnouncement(announcement);

        // Assert
        await _mockRepository.Received(1).Create(announcement);
        true.Should().BeTrue(); // Ensure at least one assertion
    }

    [TestMethod]
    public async Task CreateAnnouncement_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var announcement = CreateAnnouncement("New Announcement");
        var expectedException = new DbUpdateException("Failed to create announcement");
        _mockRepository.Create(announcement).Returns(Task.FromException(expectedException));

        // Act
        Func<Task> action = async () => await _sut.CreateAnnouncement(announcement);

        // Assert
        await action.Should().ThrowAsync<DbUpdateException>().WithMessage("Failed to create announcement");
        await _mockRepository.Received(1).Create(announcement);
    }

    #endregion

    #region UpdateAnnouncement Tests

    [TestMethod]
    public async Task UpdateAnnouncement_WithValidAnnouncement_ShouldCallRepositoryUpdate()
    {
        // Arrange
        var announcement = CreateAnnouncement("Updated Announcement");

        // Act
        await _sut.UpdateAnnouncement(announcement);

        // Assert
        await _mockRepository.Received(1).Update(announcement);
        true.Should().BeTrue(); // Ensure at least one assertion
    }

    [TestMethod]
    public async Task UpdateAnnouncement_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var announcement = CreateAnnouncement("Updated Announcement");
        var expectedException = new DbUpdateConcurrencyException("Announcement not found for update");
        _mockRepository.Update(announcement).Returns(Task.FromException(expectedException));

        // Act
        Func<Task> action = async () => await _sut.UpdateAnnouncement(announcement);

        // Assert
        await action.Should().ThrowAsync<DbUpdateConcurrencyException>().WithMessage("Announcement not found for update");
        await _mockRepository.Received(1).Update(announcement);
    }

    #endregion

    #region DeleteAnnouncement Tests
    [TestMethod]
    public async Task DeleteAnnouncement_WithValidId_ShouldCallRepositoryDelete()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        await _sut.DeleteAnnouncement(id);

        // Assert
        await _mockRepository.Received(1).Delete(id);
        true.Should().BeTrue(); // Ensure at least one assertion
    }

    [TestMethod]
    public async Task DeleteAnnouncement_WithEmptyGuid_ShouldPropagateException()
    {
        // Arrange
        var id = Guid.Empty;
        _mockRepository.Delete(id).Returns(Task.FromException(new ArgumentException("Invalid ID")));

        // Act
        Func<Task> action = async () => await _sut.DeleteAnnouncement(id);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid ID");
        await _mockRepository.Received(1).Delete(id);
    }

    [TestMethod]
    public async Task DeleteAnnouncement_WhenRepositoryThrowsNotFoundException_ShouldPropagateException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Delete(id).Returns(Task.FromException(new NotFoundException("Announcement not found")));

        // Act
        Func<Task> action = async () => await _sut.DeleteAnnouncement(id);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("Announcement not found");
        await _mockRepository.Received(1).Delete(id);
    }

    #endregion

    #region Helper Methods

    private static Announcement CreateAnnouncement(string message, Guid? id = null)
    {
        return new Announcement
        {
            Id = id ?? Guid.NewGuid(),
            CreatedOn = DateTime.Now,
            Message = message,
            Description = $"Description for {message}",
            Order = 0,
            FotoUrl = $"https://example.com/{message.Replace(" ", "-").ToLower()}.jpg",
            IsScheduled = false,
            PlannedFrom = null,
            PlannedTill = null,
            Url = $"https://example.com/{message.Replace(" ", "-").ToLower()}"
        };
    }

    #endregion
}
