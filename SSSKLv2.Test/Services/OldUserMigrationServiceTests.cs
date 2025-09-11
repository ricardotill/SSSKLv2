using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SSSKLv2.Test.Services;

[TestClass]
public class OldUserMigrationServiceTests
{
    private IOldUserMigrationRepository _mockRepository = null!;
    private ILogger<OldUserMigrationService> _mockLogger = null!;
    private OldUserMigrationService _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = Substitute.For<IOldUserMigrationRepository>();
        _mockLogger = Substitute.For<ILogger<OldUserMigrationService>>();
        _sut = new OldUserMigrationService(_mockRepository, _mockLogger);
    }

    #region GetMigrationById Tests

    [TestMethod]
    public async Task GetMigrationById_WithValidId_ReturnsMigration()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expectedMigration = CreateMigration(id, "testuser", 100m);
        _mockRepository.GetById(id).Returns(expectedMigration);

        // Act
        var result = await _sut.GetMigrationById(id);

        // Assert
        result.Should().BeEquivalentTo(expectedMigration);
        await _mockRepository.Received(1).GetById(id);
    }

    [TestMethod]
    public async Task GetMigrationById_WithNonExistentId_PropagatesNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.GetById(id).Returns(Task.FromException<OldUserMigration>(new NotFoundException("Migration not found")));

        // Act
        Func<Task> action = async () => await _sut.GetMigrationById(id);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("Migration not found");
        await _mockRepository.Received(1).GetById(id);
    }

    [TestMethod]
    public async Task GetMigrationById_WithEmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var emptyId = Guid.Empty;
        _mockRepository.GetById(emptyId).Returns(Task.FromException<OldUserMigration>(new ArgumentException("Invalid ID")));

        // Act
        Func<Task> action = async () => await _sut.GetMigrationById(emptyId);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid ID");
        await _mockRepository.Received(1).GetById(emptyId);
    }

    #endregion

    #region GetMigrationByUsername Tests

    [TestMethod]
    public async Task GetMigrationByUsername_WithValidUsername_ReturnsMigration()
    {
        // Arrange
        var username = "testuser";
        var expectedMigration = CreateMigration(Guid.NewGuid(), username, 100m);
        _mockRepository.GetByUsername(username).Returns(expectedMigration);

        // Act
        var result = await _sut.GetMigrationByUsername(username);

        // Assert
        result.Should().BeEquivalentTo(expectedMigration);
        await _mockRepository.Received(1).GetByUsername(username);
    }

    [TestMethod]
    public async Task GetMigrationByUsername_WithNonExistentUsername_PropagatesNotFoundException()
    {
        // Arrange
        var username = "nonexistentuser";
        _mockRepository.GetByUsername(username).Returns(Task.FromException<OldUserMigration>(new NotFoundException("Migration not found")));

        // Act
        Func<Task> action = async () => await _sut.GetMigrationByUsername(username);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("Migration not found");
        await _mockRepository.Received(1).GetByUsername(username);
    }

    [TestMethod]
    public async Task GetMigrationByUsername_WithNullOrEmptyUsername_ThrowsArgumentException()
    {
        // Arrange
        string nullUsername = null!;
        string emptyUsername = string.Empty;
        
        _mockRepository.GetByUsername(nullUsername).Returns(Task.FromException<OldUserMigration>(new ArgumentNullException("username")));
        _mockRepository.GetByUsername(emptyUsername).Returns(Task.FromException<OldUserMigration>(new ArgumentException("Username cannot be empty")));

        // Act & Assert
        Func<Task> nullAction = async () => await _sut.GetMigrationByUsername(nullUsername);
        await nullAction.Should().ThrowAsync<ArgumentNullException>();
        await _mockRepository.Received(1).GetByUsername(nullUsername);

        Func<Task> emptyAction = async () => await _sut.GetMigrationByUsername(emptyUsername);
        await emptyAction.Should().ThrowAsync<ArgumentException>();
        await _mockRepository.Received(1).GetByUsername(emptyUsername);
    }

    #endregion

    #region GetAll Tests

    [TestMethod]
    public async Task GetAll_ReturnsMigrations()
    {
        // Arrange
        var migrations = new List<OldUserMigration>
        {
            CreateMigration(Guid.NewGuid(), "user1", 100m),
            CreateMigration(Guid.NewGuid(), "user2", 200m)
        };
        _mockRepository.GetAll().Returns(migrations);

        // Act
        var result = await _sut.GetAll();

        // Assert
        result.Should().BeEquivalentTo(migrations);
        await _mockRepository.Received(1).GetAll();
    }

    [TestMethod]
    public async Task GetAll_WhenRepositoryReturnsEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var emptyList = new List<OldUserMigration>();
        _mockRepository.GetAll().Returns(emptyList);

        // Act
        var result = await _sut.GetAll();

        // Assert
        result.Should().BeEmpty();
        await _mockRepository.Received(1).GetAll();
    }

    [TestMethod]
    public async Task GetAll_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var exception = new InvalidOperationException("Database error");
        _mockRepository.GetAll().Returns(Task.FromException<IEnumerable<OldUserMigration>>(exception));

        // Act
        Func<Task> action = async () => await _sut.GetAll();

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database error");
        await _mockRepository.Received(1).GetAll();
    }

    #endregion

    #region CreateMigration Tests

    [TestMethod]
    public async Task CreateMigration_WithValidMigration_CallsRepository()
    {
        // Arrange
        var migration = CreateMigration(Guid.NewGuid(), "newuser", 150m);

        // Act
        await _sut.CreateMigration(migration);

        // Assert
        await _mockRepository.Received(1).Create(migration);
    }

    [TestMethod]
    public async Task CreateMigration_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var migration = CreateMigration(Guid.NewGuid(), "existinguser", 150m);
        var exception = new InvalidOperationException("User already exists");
        _mockRepository.Create(migration).Returns(Task.FromException(exception));

        // Act
        Func<Task> action = async () => await _sut.CreateMigration(migration);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("User already exists");
        await _mockRepository.Received(1).Create(migration);
    }

    #endregion

    #region DeleteMigration Tests

    [TestMethod]
    public async Task DeleteMigration_WithValidId_CallsRepository()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        await _sut.DeleteMigration(id);

        // Assert
        await _mockRepository.Received(1).Delete(id);
    }

    [TestMethod]
    public async Task DeleteMigration_WithNonExistentId_PropagatesNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Delete(id).Returns(Task.FromException(new NotFoundException("Migration not found")));

        // Act
        Func<Task> action = async () => await _sut.DeleteMigration(id);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("Migration not found");
        await _mockRepository.Received(1).Delete(id);
    }

    [TestMethod]
    public async Task DeleteMigration_WithEmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var emptyId = Guid.Empty;
        _mockRepository.Delete(emptyId).Returns(Task.FromException(new ArgumentException("Invalid ID")));

        // Act
        Func<Task> action = async () => await _sut.DeleteMigration(emptyId);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid ID");
        await _mockRepository.Received(1).Delete(emptyId);
    }

    #endregion

    #region Helper Methods

    private static OldUserMigration CreateMigration(Guid id, string username, decimal saldo)
    {
        return new OldUserMigration
        {
            Id = id,
            Username = username,
            Saldo = saldo,
            CreatedOn = DateTime.Now,
        };
    }

    #endregion
}