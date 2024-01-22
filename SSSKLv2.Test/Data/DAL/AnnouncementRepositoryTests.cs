using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class AnnouncementRepositoryTests : RepositoryTest
{
    private MockDbContextFactory _dbContextFactory = null!;
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
    public async Task GetAll_WhenDbEmpty_ReturnNoAnnouncements()
    {
        // Act
        var list = await _sut.GetAll();

        // Assert
        list.Should().BeEmpty();
    }
    
    [TestMethod]
    public async Task GetAll_WhenAnnouncementsInDb_ReturnAll()
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
    
    [TestMethod]
    public async Task GetById_WhenNotInDb_ReturnNotFoundException()
    {
        // Act
        Func<Task<Announcement>> function = () => _sut.GetById(Guid.NewGuid());

        // Assert
        await function.Should().ThrowAsync<NotFoundException>();
    }
    
    [TestMethod]
    public async Task GetById_WhenInDb_ThenReturnAnnouncement()
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
        await SaveAnnouncements(a1);

        // Act
        var result = await _sut.GetById(a1.Id);

        // Assert
        result.Should().BeEquivalentTo(a1);
    }
    
    [TestMethod]
    public async Task Create_WhenNewAnnouncement_ThenAddNewAnnouncement()
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

        // Act
        await _sut.Create(a1);

        // Assert
        var dblist = await GetAnnouncements();
        dblist.Should().HaveCount(1);
        dblist.Should().ContainEquivalentOf(a1);
    }
    
    [TestMethod]
    public async Task Create_WhenExistingAnnouncement_ThenThrowDbException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var a1 = new Announcement
        {
            Id = id,
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
            Id = id,
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
        await SaveAnnouncements(a1);

        // Act
        Func<Task> function = () => _sut.Create(a2);

        // Assert
        await function.Should().ThrowAsync<DbUpdateException>();
        var dblist = await GetAnnouncements();
        dblist.Should().ContainEquivalentOf(a1);
    }
    
    [TestMethod]
    public async Task Update_WhenExistingAnnouncement_ThenUpdate()
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
        await SaveAnnouncements(a1);
        var a1_update = (await GetAnnouncements()).FirstOrDefault(e => e.Id == a1.Id);
        a1_update.Description = "desctest2";

        // Act
        await _sut.Update(a1_update);

        // Assert
        var dblist = await GetAnnouncements();
        dblist.Should().ContainEquivalentOf(a1_update);
    }
    
    [TestMethod]
    public async Task Update_WhenNotExistingAnnouncement_ThenThrowDbException()
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

        // Act
        Func<Task> function = () => _sut.Update(a1);

        // Assert
        await function.Should().ThrowAsync<DbUpdateConcurrencyException>();
        var dblist = await GetAnnouncements();
        dblist.Should().BeEmpty();
    }
    
    [TestMethod]
    public async Task Delete_WhenExistingAnnouncement_ThenDeleteAnnouncement()
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
        await _sut.Delete(a1.Id);

        // Assert
        var dblist = await GetAnnouncements();
        dblist.Should().HaveCount(1);
    }
    
    [TestMethod]
    public async Task Delete_WhenNotExistingAnnouncement_ThenThrowDbException()
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
        await SaveAnnouncements(a1);

        // Act
        Func<Task> function = () => _sut.Delete(a2.Id);

        // Assert
        await function.Should().ThrowAsync<NotFoundException>();
        var dblist = await GetAnnouncements();
        dblist.Should().HaveCount(1);
    }
    
    private async Task<IList<Announcement>> GetAnnouncements()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Announcement.AsNoTracking().ToListAsync();
    }
    
    private async Task SaveAnnouncements(params Announcement[] announcements)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Announcement.AddRangeAsync(announcements);
        await context.SaveChangesAsync();
    }
}