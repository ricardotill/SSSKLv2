using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class EventRepositoryTests : RepositoryTest
{
    private ApplicationDbContext _context = null!;
    private EventRepository _repository = null!;

    [TestInitialize]
    public void Init()
    {
        InitializeDatabase();
        _context = new ApplicationDbContext(GetOptions());
        _repository = new EventRepository(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        CleanupDatabase();
    }

    [TestMethod]
    public async Task GetAll_OrdersFutureASC_AndPastDESC()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var creator = new ApplicationUser 
        { 
            Id = Guid.NewGuid().ToString(), 
            UserName = "test@example.com",
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            Saldo = 0
        };
        await _context.Users.AddAsync(creator);
        await _context.SaveChangesAsync();

        var pastEventEarly = new Event 
        { 
            Id = Guid.NewGuid(), 
            Title = "Past Early", 
            Description = "Desc",
            CreatorId = creator.Id,
            Creator = creator,
            StartDateTime = now.AddDays(-10), 
            EndDateTime = now.AddDays(-9),
            CreatedOn = now.AddDays(-11)
        };
        var pastEventLate = new Event 
        { 
            Id = Guid.NewGuid(), 
            Title = "Past Late", 
            Description = "Desc",
            CreatorId = creator.Id,
            Creator = creator,
            StartDateTime = now.AddDays(-5), 
            EndDateTime = now.AddDays(-4),
            CreatedOn = now.AddDays(-6)
        };
        var futureEventEarly = new Event 
        { 
            Id = Guid.NewGuid(), 
            Title = "Future Early", 
            Description = "Desc",
            CreatorId = creator.Id,
            Creator = creator,
            StartDateTime = now.AddDays(5), 
            EndDateTime = now.AddDays(6),
            CreatedOn = now.AddDays(-1)
        };
        var futureEventLate = new Event 
        { 
            Id = Guid.NewGuid(), 
            Title = "Future Late", 
            Description = "Desc",
            CreatorId = creator.Id,
            Creator = creator,
            StartDateTime = now.AddDays(10), 
            EndDateTime = now.AddDays(11),
            CreatedOn = now.AddDays(-1)
        };

        await _context.Event.AddRangeAsync(pastEventLate, futureEventLate, pastEventEarly, futureEventEarly);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAll(0, 10, false);

        // Assert
        result.Should().HaveCount(4, "all events should be returned when futureOnly is false");
        result[0].Title.Should().Be("Future Early");
        result[1].Title.Should().Be("Future Late");
        result[2].Title.Should().Be("Past Late"); // Most recent past first
        result[3].Title.Should().Be("Past Early"); // Older past last
    }

    [TestMethod]
    public async Task GetById_WithExistingId_ReturnsEvent()
    {
        // Arrange
        var e = new Event { Id = Guid.NewGuid(), Title = "Test", Description = "Desc", StartDateTime = DateTime.Now, EndDateTime = DateTime.Now.AddHours(1), CreatorId = TestUser.Id };
        await _context.Event.AddAsync(e);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetById(e.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(e.Id);
    }

    [TestMethod]
    public async Task GetById_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetById(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Add_AddsEventToDb()
    {
        // Arrange
        var e = new Event { Id = Guid.NewGuid(), Title = "Test", Description = "Desc", StartDateTime = DateTime.Now, EndDateTime = DateTime.Now.AddHours(1), CreatorId = TestUser.Id };

        // Act
        await _repository.Add(e);

        // Assert
        (await _context.Event.FindAsync(e.Id)).Should().NotBeNull();
    }

    [TestMethod]
    public async Task Update_UpdatesEventInDb()
    {
        // Arrange
        var e = new Event { Id = Guid.NewGuid(), Title = "Test", Description = "Desc", StartDateTime = DateTime.Now, EndDateTime = DateTime.Now.AddHours(1), CreatorId = TestUser.Id };
        await _context.Event.AddAsync(e);
        await _context.SaveChangesAsync();
        e.Title = "Updated";

        // Act
        await _repository.Update(e);

        // Assert
        var updated = await _context.Event.FindAsync(e.Id);
        updated!.Title.Should().Be("Updated");
    }

    [TestMethod]
    public async Task Delete_RemovesEventFromDb()
    {
        // Arrange
        var e = new Event { Id = Guid.NewGuid(), Title = "Test", Description = "Desc", StartDateTime = DateTime.Now, EndDateTime = DateTime.Now.AddHours(1), CreatorId = TestUser.Id };
        await _context.Event.AddAsync(e);
        await _context.SaveChangesAsync();

        // Act
        await _repository.Delete(e.Id);

        // Assert
        (await _context.Event.FindAsync(e.Id)).Should().BeNull();
    }

    [TestMethod]
    public async Task GetCount_ReturnsCorrectCount()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var e1 = new Event { Id = Guid.NewGuid(), Title = "Future", StartDateTime = now.AddDays(1), EndDateTime = now.AddDays(2), Description = "D", CreatorId = TestUser.Id };
        var e2 = new Event { Id = Guid.NewGuid(), Title = "Past", StartDateTime = now.AddDays(-2), EndDateTime = now.AddDays(-1), Description = "D", CreatorId = TestUser.Id };
        await _context.Event.AddRangeAsync(e1, e2);
        await _context.SaveChangesAsync();

        // Act & Assert
        (await _repository.GetCount(futureOnly: false)).Should().Be(2);
        (await _repository.GetCount(futureOnly: true)).Should().Be(1);
    }

    [TestMethod]
    public async Task Responses_CRUD_Operations()
    {
        // Arrange
        var e = new Event { Id = Guid.NewGuid(), Title = "Test", Description = "D", StartDateTime = DateTime.Now, EndDateTime = DateTime.Now.AddHours(1), CreatorId = TestUser.Id };
        var u = new ApplicationUser { Id = "user2", UserName = "u2", Email = "u2@e.com", Name = "N", Surname = "S" };
        await _context.Event.AddAsync(e);
        await _context.Users.AddAsync(u);
        await _context.SaveChangesAsync();

        var response = new EventResponse { EventId = e.Id, UserId = u.Id, Status = EventResponseStatus.Accepted, CreatedOn = DateTime.Now };

        // Act: Add
        await _repository.AddResponse(response);
        (await _repository.GetResponse(e.Id, u.Id)).Should().NotBeNull();

        // Act: Update
        response.Status = EventResponseStatus.Declined;
        await _repository.UpdateResponse(response);
        (await _repository.GetResponse(e.Id, u.Id))!.Status.Should().Be(EventResponseStatus.Declined);

        // Act: Delete
        await _repository.DeleteResponse(response);
        (await _repository.GetResponse(e.Id, u.Id)).Should().BeNull();
    }
}
