using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class OldUserMigrationRepositoryTests : RepositoryTest
{
    private IDbContextFactory<ApplicationDbContext> _dbContextFactory = null!;
    private OldUserMigrationRepository _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContextFactory = new MockDbContextFactory(GetOptions());
        _sut = new OldUserMigrationRepository(_dbContextFactory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
    }

    [TestMethod]
    public async Task GetAll_WhenOldUserMigrationsInDb_ReturnAll()
    {
        // Arrange
        var o1 = new OldUserMigration()
        {
            Id = Guid.NewGuid(),
            Saldo = 20.95m,
            Username = "username1",
            CreatedOn = DateTime.Now
        };
        var o2 = new OldUserMigration()
        {
            Id = Guid.NewGuid(),
            Saldo = 10.05m,
            Username = "username2",
            CreatedOn = DateTime.Now.AddMonths(-1)
        };
        await SaveOldUserMigrations(o1, o2);
        
        // Act
        var result = await _sut.GetAll();

        // Assert
        var oldUserMigrations = result as OldUserMigration[] ?? result.ToArray();
        oldUserMigrations.Should().HaveCount(2);
        oldUserMigrations.Should().ContainEquivalentOf(o1);
        oldUserMigrations.Should().ContainEquivalentOf(o2);
    }
    
    [TestMethod]
    public async Task GetAll_WhenDbEmpty_ReturnNoOldUserMigrations()
    {
        // Act
        var result = await _sut.GetAll();
        
        // Assert
        var oldUserMigrations = result as OldUserMigration[] ?? result.ToArray();
        oldUserMigrations.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetById_WhenInDb_ThenReturnOldUserMigration()
    {
        // Arrange
        var o1 = new OldUserMigration()
        {
            Id = Guid.NewGuid(),
            Saldo = 20.95m,
            Username = "username",
            CreatedOn = DateTime.Now
        };
        await SaveOldUserMigrations(o1);
        
        // Act
        var result = await _sut.GetById(o1.Id);

        // Assert
        result.Should().BeEquivalentTo(o1);
    }

    [TestMethod]
    public async Task GetById_WhenNotInDb_ReturnNotFoundException()
    {
        // Act
        Func<Task<OldUserMigration>> function = () => _sut.GetById(Guid.NewGuid());

        // Assert
        await function.Should().ThrowAsync<NotFoundException>();
    }
    
    [TestMethod]
    public async Task GetByUsername_WhenInDb_ThenReturnOldUserMigration()
    {
        // Arrange
        var o1 = new OldUserMigration()
        {
            Id = Guid.NewGuid(),
            Saldo = 20.95m,
            Username = "username",
            CreatedOn = DateTime.Now
        };
        await SaveOldUserMigrations(o1);
        
        // Act
        var result = await _sut.GetByUsername(o1.Username);

        // Assert
        result.Should().BeEquivalentTo(o1);
    }

    [TestMethod]
    public async Task GetByUsername_WhenNotInDb_ReturnNotFoundException()
    {
        // Act
        Func<Task<OldUserMigration>> function = () => _sut.GetByUsername("username");

        // Assert
        await function.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Create_WhenNewAnnouncement_ThenAddNewAnnouncement()
    {
        // Arrange
        var o1 = new OldUserMigration()
        {
            Id = Guid.NewGuid(),
            Saldo = 20.95m,
            Username = "username",
            CreatedOn = DateTime.Now
        };
        
        // Act
        await _sut.Create(o1);
        
        // Assert
        var dblist = await GetOldUserMigrations();
        dblist.Should().HaveCount(1);
        dblist.Should().ContainEquivalentOf(o1);
    }

    [TestMethod]
    public async Task Create_WhenExistingId_ThenThrowDbException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var o1 = new OldUserMigration()
        {
            Id = id,
            Saldo = 20.95m,
            Username = "username1",
            CreatedOn = DateTime.Now
        };
        var o2 = new OldUserMigration()
        {
            Id = id,
            Saldo = 20.95m,
            Username = "username2",
            CreatedOn = DateTime.Now
        };
        await SaveOldUserMigrations(o1);
        
        // Act
        Func<Task> function = () => _sut.Create(o2);

        // Assert
        await function.Should().ThrowAsync<DbUpdateException>();
        var dblist = await GetOldUserMigrations();
        dblist.Should().NotContainEquivalentOf(o2);
        dblist.Should().HaveCount(1);
    }
    
    [TestMethod]
    public async Task Create_WhenExistingUsername_ThenThrowDbException()
    {
        // Arrange
        var username = "username";
        var o1 = new OldUserMigration()
        {
            Id = Guid.NewGuid(),
            Saldo = 20.95m,
            Username = username,
            CreatedOn = DateTime.Now
        };
        var o2 = new OldUserMigration()
        {
            Id = Guid.NewGuid(),
            Saldo = 20.95m,
            Username = username,
            CreatedOn = DateTime.Now
        };
        await SaveOldUserMigrations(o1);
        
        // Act
        Func<Task> function = () => _sut.Create(o2);

        // Assert
        await function.Should().ThrowAsync<DbUpdateException>();
        var dblist = await GetOldUserMigrations();
        dblist.Should().NotContainEquivalentOf(o2);
        dblist.Should().HaveCount(1);
    }
    
    [TestMethod]
    public async Task Delete_WhenExistingOldUserMigration_ThenDeleteOldUserMigration()
    {
        // Arrange
        var o1 = new OldUserMigration()
        {
            Id = Guid.NewGuid(),
            Saldo = 20.95m,
            Username = "username1",
            CreatedOn = DateTime.Now
        };
        var o2 = new OldUserMigration()
        {
            Id = Guid.NewGuid(),
            Saldo = 20.95m,
            Username = "username2",
            CreatedOn = DateTime.Now
        };
        await SaveOldUserMigrations(o1, o2);

        // Act
        await _sut.Delete(o1.Id);

        // Assert
        var dblist = await GetOldUserMigrations();
        dblist.Should().NotContainEquivalentOf(o1);
        dblist.Should().HaveCount(1);
    }
    
    [TestMethod]
    public async Task Delete_WhenNotExistingOldUserMigration_ThenThrowDbException()
    {
        // Arrange
        var o1 = new OldUserMigration()
        {
            Id = Guid.NewGuid(),
            Saldo = 20.95m,
            Username = "username1",
            CreatedOn = DateTime.Now
        };
        var o2 = new OldUserMigration()
        {
            Id = Guid.NewGuid(),
            Saldo = 20.95m,
            Username = "username2",
            CreatedOn = DateTime.Now
        };
        await SaveOldUserMigrations(o1);

        // Act
        Func<Task> function = () => _sut.Delete(o2.Id);

        // Assert
        await function.Should().ThrowAsync<NotFoundException>();
        var dblist = await GetOldUserMigrations();
        dblist.Should().HaveCount(1);
    }

    private async Task<IList<OldUserMigration>> GetOldUserMigrations()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.OldUserMigration.AsNoTracking().ToListAsync();
    }
    
    private async Task SaveOldUserMigrations(params OldUserMigration[] oldUserMigrations)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.OldUserMigration.AddRangeAsync(oldUserMigrations);
        await context.SaveChangesAsync();
    }
}