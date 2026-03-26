using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class EventRepositoryTests
{
    private ApplicationDbContext _context = null!;
    private EventRepository _repository = null!;

    [TestInitialize]
    public void Init()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new EventRepository(_context);
    }

    [TestMethod]
    public async Task GetAll_OrdersFutureBeforePast_AndChronological()
    {
        // Arrange
        var now = DateTime.Now;
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
        result[2].Title.Should().Be("Past Early");
        result[3].Title.Should().Be("Past Late");
    }
}
