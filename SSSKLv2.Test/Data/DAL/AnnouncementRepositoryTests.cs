using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class AnnouncementRepositoryTests : RepositoryTest
{
    private IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private AnnouncementRepository _sut;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContextFactory = new MockDbContextFactory(GetOptions());
        _sut = new AnnouncementRepository(_dbContextFactory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
    }

    [TestMethod]
    public async Task GetAll_Should_ReturnNoAnnouncements()
    {
        // Act
        var list = await _sut.GetAll();

        // Assert
        list.Should().BeEmpty();
    }
    
    [TestMethod]
    public async Task GetAll_Should_ReturnAll()
    {
        // Arrange
        var a1 = new Announcement
        {
            Id = Guid.NewGuid(),
            CreatedOn = DateTime.Now,
            Message = "message1",
            Description = "desctest1",
            Order = 0,
            FotoUrl = "https://foto1/",
            IsScheduled = false,
            PlannedFrom = null,
            PlannedTill = null,
            Url = "https://url1/"
        };
        var a2 = new Announcement
        {
            Id = Guid.NewGuid(),
            CreatedOn = DateTime.Now,
            Message = "message2",
            Description = "desctest2",
            Order = 5,
            FotoUrl = "https://foto2/",
            IsScheduled = false,
            PlannedFrom = null,
            PlannedTill = null,
            Url = "https://url2/"
        };
        await SaveAnnouncements(a1, a2);

        // Act
        var list = await _sut.GetAll();

        // Assert
        list.Should().HaveCount(2);
        list.Should().ContainEquivalentOf(a1);
        list.Should().ContainEquivalentOf(a2);
    }
    
    private async Task SaveAnnouncements(params Announcement[] announcements)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        await context.Announcement.AddRangeAsync(announcements);
        await context.SaveChangesAsync();
    }

    private void CreateTestData()
    {
        using var context = _dbContextFactory.CreateDbContext();
        var r1 = new Announcement
        {
            Id = Guid.NewGuid(),
            CreatedOn = DateTime.Now,
            Message = "message1",
            Description = "desctest1",
            Order = 0,
            FotoUrl = "https://foto1/",
            IsScheduled = false,
            PlannedFrom = null,
            PlannedTill = null,
            Url = "https://url1/"
        };
        var r2 = new Announcement
        {
            Id = Guid.NewGuid(),
            CreatedOn = DateTime.Now,
            Message = "message2",
            Description = "desctest2",
            Order = 5,
            FotoUrl = "https://foto2/",
            IsScheduled = false,
            PlannedFrom = null,
            PlannedTill = null,
            Url = "https://url2/"
        };
        var date = DateTime.Now;
        var r3 = new Announcement
        {
            Id = Guid.NewGuid(),
            CreatedOn = date,
            Message = "message3",
            Description = "desctest3",
            Order = 2,
            FotoUrl = "https://foto3/",
            IsScheduled = true,
            PlannedFrom = date.AddDays(-10),
            PlannedTill = date.AddDays(1),
            Url = "https://url3/"
        };
        context.Announcement.AddRange(r1, r2, r3);
        context.SaveChanges();
    }
}