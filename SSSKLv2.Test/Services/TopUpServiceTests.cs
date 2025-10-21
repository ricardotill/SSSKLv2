using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Test.Services;

[TestClass]
public class TopUpServiceTests
{
    private ITopUpRepository _mockRepository = null!;
    private IAchievementService _achievementService = null!;
    private ILogger<TopUpService> _mockLogger = null!;
    private TopUpService _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = Substitute.For<ITopUpRepository>();
        _achievementService = Substitute.For<IAchievementService>();
        _mockLogger = Substitute.For<ILogger<TopUpService>>();
        _sut = new TopUpService(_mockRepository, _achievementService, _mockLogger);
    }

    #region GetAllQueryable Tests

    [TestMethod]
    public void GetAllQueryable_ShouldReturnQueryableFromRepository()
    {
        // Arrange
        var topUps = new List<TopUp>
        {
            CreateTopUp(Guid.NewGuid(), "user1", 100m),
            CreateTopUp(Guid.NewGuid(), "user2", 200m)
        }.AsQueryable();

        _mockRepository.GetAllQueryable(Arg.Any<ApplicationDbContext>()).Returns(topUps);

        // Act
        var result = _sut.GetAllQueryable(null!);

        // Assert
        result.Should().BeEquivalentTo(topUps);
        _mockRepository.Received(1).GetAllQueryable(null!);
    }

    [TestMethod]
    public void GetAllQueryable_WhenRepositoryReturnsEmptyQueryable_ShouldReturnEmptyQueryable()
    {
        // Arrange
        var emptyQueryable = new List<TopUp>().AsQueryable();
        _mockRepository.GetAllQueryable(Arg.Any<ApplicationDbContext>()).Returns(emptyQueryable);

        // Act
        var result = _sut.GetAllQueryable(null!);

        // Assert
        result.Should().BeEmpty();
        _mockRepository.Received(1).GetAllQueryable(Arg.Any<ApplicationDbContext>());
    }

    [TestMethod]
    public void GetAllQueryable_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        _mockRepository.GetAllQueryable(Arg.Any<ApplicationDbContext>()).Throws(
            new InvalidOperationException("Database error"));

        // Act
        Action action = () => _sut.GetAllQueryable(null!);

        // Assert
        action.Should().Throw<InvalidOperationException>().WithMessage("Database error");
        _mockRepository.Received(1).GetAllQueryable(Arg.Any<ApplicationDbContext>());
    }

    [TestMethod]
    public void GetAllQueryable_WhenDbContextIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        ApplicationDbContext nullContext = null!;
        _mockRepository.GetAllQueryable(nullContext).Throws(
            new ArgumentNullException(nameof(nullContext)));

        // Act
        Action action = () => _sut.GetAllQueryable(nullContext);

        // Assert
        action.Should().Throw<ArgumentNullException>();
        _mockRepository.Received(1).GetAllQueryable(nullContext);
    }

    #endregion

    #region GetPersonalQueryable Tests

    [TestMethod]
    public void GetPersonalQueryable_WithValidUsername_ShouldReturnQueryableFromRepository()
    {
        // Arrange
        var username = "testUser";
        var topUps = new List<TopUp>
        {
            CreateTopUp(Guid.NewGuid(), username, 100m),
            CreateTopUp(Guid.NewGuid(), username, 50m)
        }.AsQueryable();

        _mockRepository.GetPersonalQueryable(username, Arg.Any<ApplicationDbContext>()).Returns(topUps);

        // Act
        var result = _sut.GetPersonalQueryable(username, null!);

        // Assert
        result.Should().BeEquivalentTo(topUps);
        _mockRepository.Received(1).GetPersonalQueryable(username, Arg.Any<ApplicationDbContext>());
    }

    [TestMethod]
    public void GetPersonalQueryable_WhenRepositoryReturnsEmptyQueryable_ShouldReturnEmptyQueryable()
    {
        // Arrange
        var username = "testUser";
        var emptyQueryable = new List<TopUp>().AsQueryable();
        _mockRepository.GetPersonalQueryable(username, Arg.Any<ApplicationDbContext>()).Returns(emptyQueryable);

        // Act
        var result = _sut.GetPersonalQueryable(username, null!);

        // Assert
        result.Should().BeEmpty();
        _mockRepository.Received(1).GetPersonalQueryable(username, Arg.Any<ApplicationDbContext>());
    }

    [TestMethod]
    public void GetPersonalQueryable_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var username = "testUser";
        _mockRepository.GetPersonalQueryable(username, Arg.Any<ApplicationDbContext>()).Throws(
            new InvalidOperationException("Database error"));

        // Act
        Action action = () => _sut.GetPersonalQueryable(username, null!);

        // Assert
        action.Should().Throw<InvalidOperationException>().WithMessage("Database error");
        _mockRepository.Received(1).GetPersonalQueryable(username, Arg.Any<ApplicationDbContext>());
    }

    [TestMethod]
    public void GetPersonalQueryable_WhenUsernameIsNullOrEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        string nullUsername = null!;
        string emptyUsername = string.Empty;
        
        _mockRepository.GetPersonalQueryable(nullUsername, Arg.Any<ApplicationDbContext>()).Throws(
            new ArgumentNullException(nameof(nullUsername)));
            
        _mockRepository.GetPersonalQueryable(emptyUsername, Arg.Any<ApplicationDbContext>()).Throws(
            new ArgumentException("Username cannot be empty", nameof(emptyUsername)));

        // Act & Assert
        Action nullAction = () => _sut.GetPersonalQueryable(nullUsername, null!);
        nullAction.Should().Throw<ArgumentNullException>();
        
        Action emptyAction = () => _sut.GetPersonalQueryable(emptyUsername, null!);
        emptyAction.Should().Throw<ArgumentException>().WithMessage("*Username cannot be empty*");
        
        _mockRepository.Received(1).GetPersonalQueryable(nullUsername, Arg.Any<ApplicationDbContext>());
        _mockRepository.Received(1).GetPersonalQueryable(emptyUsername, Arg.Any<ApplicationDbContext>());
    }

    [TestMethod]
    public void GetPersonalQueryable_WhenDbContextIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var username = "testUser";
        ApplicationDbContext nullContext = null!;
        
        _mockRepository.GetPersonalQueryable(username, nullContext).Throws(
            new ArgumentNullException("dbContext"));

        // Act
        Action action = () => _sut.GetPersonalQueryable(username, nullContext);

        // Assert
        action.Should().Throw<ArgumentNullException>();
        _mockRepository.Received(1).GetPersonalQueryable(username, nullContext);
    }

    #endregion

    #region GetById Tests

    [TestMethod]
    public async Task GetById_WithValidId_ShouldReturnTopUp()
    {
        // Arrange
        var id = Guid.NewGuid();
        var idString = id.ToString();
        var expectedTopUp = CreateTopUp(id, "testUser", 100m);
        
        _mockRepository.GetById(id).Returns(expectedTopUp);

        // Act
        var result = await _sut.GetById(idString);

        // Assert
        result.Should().BeEquivalentTo(expectedTopUp);
        await _mockRepository.Received(1).GetById(id);
    }

    [TestMethod]
    public async Task GetById_WithNonExistentId_ShouldPropagateNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var idString = id.ToString();
        _mockRepository.GetById(id).Throws(new NotFoundException("TopUp not found"));

        // Act
        Func<Task> action = async () => await _sut.GetById(idString);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("TopUp not found");
        await _mockRepository.Received(1).GetById(id);
    }

    [TestMethod]
    public async Task GetById_WithEmptyGuid_ShouldPropagateException()
    {
        // Arrange
        var emptyGuid = Guid.Empty;
        var emptyGuidString = emptyGuid.ToString();
        _mockRepository.GetById(emptyGuid).Throws(new ArgumentException("Invalid TopUp ID"));

        // Act
        Func<Task> action = async () => await _sut.GetById(emptyGuidString);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid TopUp ID");
        await _mockRepository.Received(1).GetById(emptyGuid);
    }

    [TestMethod]
    public void GetById_WithInvalidGuidFormat_ShouldThrowFormatException()
    {
        // Arrange
        var invalidIdString = "not-a-guid";

        // Act
        Func<Task> action = async () => await _sut.GetById(invalidIdString);

        // Assert
        action.Should().ThrowAsync<FormatException>();
        // Repository should not be called with an invalid GUID
        _mockRepository.DidNotReceiveWithAnyArgs().GetById(Arg.Any<Guid>());
    }

    [TestMethod]
    public async Task GetById_WithNullOrEmptyIdString_ShouldThrowArgumentException()
    {
        // Arrange
        string nullId = null!;
        string emptyId = string.Empty;

        // Act & Assert
        Func<Task> nullAction = async () => await _sut.GetById(nullId);
        await nullAction.Should().ThrowAsync<ArgumentNullException>();
        
        Func<Task> emptyAction = async () => await _sut.GetById(emptyId);
        await emptyAction.Should().ThrowAsync<FormatException>();
        
        // Repository should not be called with invalid inputs
        await _mockRepository.DidNotReceiveWithAnyArgs().GetById(Arg.Any<Guid>());
    }

    #endregion

    #region CreateTopUp Tests

    [TestMethod]
    public async Task CreateTopUp_WithValidTopUp_ShouldCallRepository()
    {
        // Arrange
        var topUp = CreateTopUp(Guid.NewGuid(), "testUser", 100m);

        // Act
        await _sut.CreateTopUp(topUp);

        // Assert
        await _mockRepository.Received(1).Create(topUp);
    }

    [TestMethod]
    public async Task CreateTopUp_WithTopUpWithoutUser_ShouldPropagateException()
    {
        // Arrange
        var topUpWithoutUser = new TopUp
        {
            Id = Guid.NewGuid(),
            User = null!, // No user
            Saldo = 100m,
            CreatedOn = DateTime.Now
        };
        
        _mockRepository.Create(topUpWithoutUser).Throws(
            new ArgumentException("TopUp must have a User"));

        // Act
        Func<Task> action = async () => await _sut.CreateTopUp(topUpWithoutUser);

        // Assert
        await action.Should().ThrowAsync<NullReferenceException>();
    }

    [TestMethod]
    public async Task CreateTopUp_WithNegativeSaldo_ShouldStillCallRepository()
    {
        // Arrange
        var topUpWithNegativeSaldo = CreateTopUp(Guid.NewGuid(), "testUser", -50m);

        // Act
        await _sut.CreateTopUp(topUpWithNegativeSaldo);

        // Assert
        await _mockRepository.Received(1).Create(topUpWithNegativeSaldo);
    }

    [TestMethod]
    public async Task CreateTopUp_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var topUp = CreateTopUp(Guid.NewGuid(), "testUser", 100m);
        var expectedException = new DbUpdateException("Failed to create top-up");
        _mockRepository.Create(topUp).Throws(expectedException);

        // Act
        Func<Task> action = async () => await _sut.CreateTopUp(topUp);

        // Assert
        await action.Should().ThrowAsync<DbUpdateException>().WithMessage("Failed to create top-up");
        await _mockRepository.Received(1).Create(topUp);
    }

    #endregion

    #region DeleteTopUp Tests

    [TestMethod]
    public async Task DeleteTopUp_WithValidId_ShouldCallRepository()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        await _sut.DeleteTopUp(id);

        // Assert
        await _mockRepository.Received(1).Delete(id);
    }

    [TestMethod]
    public async Task DeleteTopUp_WithEmptyGuid_ShouldPropagateException()
    {
        // Arrange
        var emptyGuid = Guid.Empty;
        _mockRepository.Delete(emptyGuid).Throws(new ArgumentException("Invalid TopUp ID"));

        // Act
        Func<Task> action = async () => await _sut.DeleteTopUp(emptyGuid);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid TopUp ID");
        await _mockRepository.Received(1).Delete(emptyGuid);
    }

    [TestMethod]
    public async Task DeleteTopUp_WhenRepositoryThrowsNotFoundException_ShouldPropagateException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Delete(id).Throws(new NotFoundException("TopUp not found"));

        // Act
        Func<Task> action = async () => await _sut.DeleteTopUp(id);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("TopUp not found");
        await _mockRepository.Received(1).Delete(id);
    }

    [TestMethod]
    public async Task DeleteTopUp_WhenRepositoryThrowsUnexpectedException_ShouldPropagateException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expectedException = new InvalidOperationException("Database error");
        _mockRepository.Delete(id).Throws(expectedException);

        // Act
        Func<Task> action = async () => await _sut.DeleteTopUp(id);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database error");
        await _mockRepository.Received(1).Delete(id);
    }

    #endregion

    #region Helper Methods

    private static TopUp CreateTopUp(Guid id, string username, decimal saldo)
    {
        return new TopUp
        {
            Id = id,
            User = new ApplicationUser { UserName = username },
            Saldo = saldo,
            CreatedOn = DateTime.UtcNow
        };
    }

    #endregion
}