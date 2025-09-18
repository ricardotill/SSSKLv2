using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using SSSKLv2.Test.Util;
using FluentAssertions;
using NSubstitute;
using SSSKLv2.Data.DAL.Exceptions;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class TopUpRepositoryTests : RepositoryTest
{
    private MockDbContextFactory _dbContextFactory = null!;
    private TopUpRepository _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContextFactory = new MockDbContextFactory(GetOptions());
        _sut = new TopUpRepository(_dbContextFactory);
        
        // Clear any existing TopUps for test isolation
        ClearAllTopUps().GetAwaiter().GetResult();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
    }
    
    #region GetAllQueryable Tests
    
    [TestMethod]
    public async Task GetAllQueryable_WhenTopUpsExist_ReturnsOrderedByCreatedOnDescending()
    {
        // Arrange
        var user = TestUser;
        var topUp1 = CreateTopUp(user, 10m, DateTime.Now.AddHours(-2));
        var topUp2 = CreateTopUp(user, 20m, DateTime.Now.AddHours(-1));
        var topUp3 = CreateTopUp(user, 30m, DateTime.Now);
        await SaveTopUps(topUp1, topUp2, topUp3);
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        // Act
        var result = _sut.GetAllQueryable(context);
        var materializedResult = await result.ToListAsync();
        
        // Assert
        materializedResult.Should().HaveCount(3);
        materializedResult[0].Id.Should().Be(topUp3.Id); // Most recent first
        materializedResult[1].Id.Should().Be(topUp2.Id);
        materializedResult[2].Id.Should().Be(topUp1.Id);
    }
    
    [TestMethod]
    public async Task GetAllQueryable_WhenNoTopUpsExist_ReturnsEmptyList()
    {
        // Arrange
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        // Act
        var result = _sut.GetAllQueryable(context);
        var materializedResult = await result.ToListAsync();
        
        // Assert
        materializedResult.Should().BeEmpty();
    }
    
    #endregion
    
    #region GetPersonalQueryable Tests
    
    [TestMethod]
    public async Task GetPersonalQueryable_WhenUserHasTopUps_ReturnsOnlyUsersTopUpsOrderedByCreatedOnDescending()
    {
        // Arrange
        var user1 = TestUser; // Username: testuser
        var user2 = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "otheruser",
            Email = "other@test.com",
            Name = "Other",
            Surname = "User"
        };
        await SaveUser(user2);
        
        var topUp1 = CreateTopUp(user1, 10m, DateTime.Now.AddHours(-2));
        var topUp2 = CreateTopUp(user1, 20m, DateTime.Now);
        var topUp3 = CreateTopUp(user2, 30m, DateTime.Now.AddHours(-1));
        
        await SaveTopUps(topUp1, topUp2, topUp3);
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        // Act
        var result = _sut.GetPersonalQueryable("testuser", context);
        var materializedResult = await result.ToListAsync();
        
        // Assert
        materializedResult.Should().HaveCount(2);
        materializedResult.Should().Contain(t => t.Id == topUp1.Id);
        materializedResult.Should().Contain(t => t.Id == topUp2.Id);
        materializedResult.Should().NotContain(t => t.Id == topUp3.Id);
        
        // Verify descending order
        materializedResult[0].Id.Should().Be(topUp2.Id); // Most recent first
        materializedResult[1].Id.Should().Be(topUp1.Id);
    }
    
    [TestMethod]
    public async Task GetPersonalQueryable_WhenUserHasNoTopUps_ReturnsEmptyList()
    {
        // Arrange
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        // Act
        var result = _sut.GetPersonalQueryable("testuser", context);
        var materializedResult = await result.ToListAsync();
        
        // Assert
        materializedResult.Should().BeEmpty();
    }
    
    [TestMethod]
    public async Task GetPersonalQueryable_WithNonExistingUser_ReturnsEmptyList()
    {
        // Arrange
        var user = TestUser;
        var topUp = CreateTopUp(user, 10m);
        await SaveTopUps(topUp);
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        // Act
        var result = _sut.GetPersonalQueryable("nonexistentuser", context);
        var materializedResult = await result.ToListAsync();
        
        // Assert
        materializedResult.Should().BeEmpty();
    }
    
    #endregion
    
    #region GetById Tests
    
    [TestMethod]
    public async Task GetById_WithExistingId_ReturnsTopUp()
    {
        // Arrange
        var user = TestUser;
        var topUp = CreateTopUp(user, 50m);
        await SaveTopUps(topUp);
        
        // Act
        var result = await _sut.GetById(topUp.Id);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(topUp.Id);
        result.Saldo.Should().Be(50m);
        result.User.Should().NotBeNull();
        result.User.UserName.Should().Be("testuser");
    }
    
    [TestMethod]
    public async Task GetById_WithNonExistentId_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        
        // Act
        Func<Task> action = async () => await _sut.GetById(nonExistentId);
        
        // Assert
        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("TopUp not found");
    }
    
    #endregion
    
    #region Create Tests
    
    [TestMethod]
    public async Task Create_AddsTopUpAndUpdatesUserSaldo()
    {
        // Arrange
        var user = TestUser;
        user.Saldo = 100m;
        await UpdateUser(user);
        
        var topUp = CreateTopUp(user, 50m);
        
        // Act
        await _sut.Create(topUp);
        
        // Assert
        // Verify TopUp was added
        var savedTopUps = await GetTopUpsDirectly();
        savedTopUps.Should().ContainSingle();
        savedTopUps[0].Id.Should().Be(topUp.Id);
        savedTopUps[0].Saldo.Should().Be(50m);
        
        // Verify user saldo was updated
        var updatedUser = await GetUserById(user.Id);
        updatedUser.Saldo.Should().Be(150m); // 100 (original) + 50 (top-up)
    }
    
    [TestMethod]
    public async Task Create_WithNegativeSaldo_StillAddsAndIncrementsUserSaldo()
    {
        // Arrange
        var user = TestUser;
        user.Saldo = 100m;
        await UpdateUser(user);
        
        var topUp = CreateTopUp(user, -30m); // Negative top-up
        
        // Act
        await _sut.Create(topUp);
        
        // Assert
        // Verify TopUp was added
        var savedTopUps = await GetTopUpsDirectly();
        savedTopUps.Should().ContainSingle();
        savedTopUps[0].Saldo.Should().Be(-30m);
        
        // Verify user saldo was updated (decreased)
        var updatedUser = await GetUserById(user.Id);
        updatedUser.Saldo.Should().Be(70m); // 100 (original) + (-30) (top-up)
    }
    
    [TestMethod]
    public async Task Create_WithZeroSaldo_AddsTopUpButDoesntChangeUserSaldo()
    {
        // Arrange
        var user = TestUser;
        user.Saldo = 100m;
        await UpdateUser(user);
        
        var topUp = CreateTopUp(user, 0m); // Zero amount
        
        // Act
        await _sut.Create(topUp);
        
        // Assert
        // Verify TopUp was added
        var savedTopUps = await GetTopUpsDirectly();
        savedTopUps.Should().ContainSingle();
        savedTopUps[0].Saldo.Should().Be(0m);
        
        // Verify user saldo remains unchanged
        var updatedUser = await GetUserById(user.Id);
        updatedUser.Saldo.Should().Be(100m); // Original saldo + 0
    }
    
    #endregion
    
    #region Delete Tests
    
    [TestMethod]
    public async Task Delete_WithExistingId_RemovesTopUpAndRevertsUserSaldo()
    {
        // Arrange
        var user = TestUser;
        user.Saldo = 150m; // Saldo after top-up has been applied
        await UpdateUser(user);
        
        var topUp = CreateTopUp(user, 50m);
        await SaveTopUps(topUp);
        
        // Act
        await _sut.Delete(topUp.Id);
        
        // Assert
        // Verify TopUp was removed
        var remainingTopUps = await GetTopUpsDirectly();
        remainingTopUps.Should().BeEmpty();
        
        // Verify user saldo was reverted
        var updatedUser = await GetUserById(user.Id);
        updatedUser.Saldo.Should().Be(100m); // 150 (after top-up) - 50 (removed top-up)
    }
    
    [TestMethod]
    public async Task Delete_WithNegativeSaldo_RemovesTopUpAndIncrementsUserSaldo()
    {
        // Arrange
        var user = TestUser;
        user.Saldo = 70m; // Saldo after negative top-up has been applied
        await UpdateUser(user);
        
        var topUp = CreateTopUp(user, -30m); // Negative top-up
        await SaveTopUps(topUp);
        
        // Act
        await _sut.Delete(topUp.Id);
        
        // Assert
        // Verify TopUp was removed
        var remainingTopUps = await GetTopUpsDirectly();
        remainingTopUps.Should().BeEmpty();
        
        // Verify user saldo was incremented (since we're removing a negative top-up)
        var updatedUser = await GetUserById(user.Id);
        updatedUser.Saldo.Should().Be(100m); // 70 (after negative top-up) - (-30) (removed negative top-up)
    }
    
    [TestMethod]
    public async Task Delete_WithNonExistentId_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        
        // Act
        Func<Task> action = async () => await _sut.Delete(nonExistentId);
        
        // Assert
        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("TopUp Not Found"); // Note the exact message has specific capitalization
    }
    
    #endregion
    
    #region Mock Factory Tests
    
    [TestMethod]
    public async Task Repository_WhenDbContextFactoryThrowsException_PropagatesException()
    {
        // Arrange
        var mockFactory = Substitute.For<IDbContextFactory<ApplicationDbContext>>();
        mockFactory.CreateDbContextAsync().Returns(Task.FromException<ApplicationDbContext>(new InvalidOperationException("Database connection error")));
        var repository = new TopUpRepository(mockFactory);
        
        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => repository.GetById(Guid.NewGuid()));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => repository.Create(new TopUp()));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => repository.Delete(Guid.NewGuid()));
    }
    
    #endregion
    
    #region Helper Methods
    
    private TopUp CreateTopUp(ApplicationUser user, decimal saldo, DateTime? createdOn = null)
    {
        return new TopUp
        {
            Id = Guid.NewGuid(),
            User = user,
            Saldo = saldo,
            CreatedOn = createdOn ?? DateTime.Now
        };
    }
    
    private async Task<List<TopUp>> GetTopUpsDirectly()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.TopUp
            .Include(t => t.User)
            .AsNoTracking()
            .ToListAsync();
    }
    
    private async Task SaveTopUps(params TopUp[] topUps)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        foreach (var topUp in topUps)
        {
            // Make sure we're using the already tracked user entity
            if (topUp.User != null)
            {
                var existingUser = await context.Users.FindAsync(topUp.User.Id);
                if (existingUser != null)
                {
                    topUp.User = existingUser;
                }
            }
            
            await context.TopUp.AddAsync(topUp);
        }
        
        await context.SaveChangesAsync();
    }
    
    private async Task SaveUser(ApplicationUser user)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
    }
    
    private async Task UpdateUser(ApplicationUser user)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }
    
    private async Task<ApplicationUser> GetUserById(string id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Users.FindAsync(id) ?? throw new InvalidOperationException($"User with ID {id} not found");
    }
    
    private async Task ClearAllTopUps()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.TopUp.RemoveRange(context.TopUp);
        await context.SaveChangesAsync();
    }
    
    #endregion
}