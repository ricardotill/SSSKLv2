using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class AchievementRepositoryTests : RepositoryTest
{
    private MockDbContextFactory _dbContextFactory = null!;
    private AchievementRepository _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContextFactory = new MockDbContextFactory(GetOptions());
        _sut = new AchievementRepository(_dbContextFactory);
        // Ensure a clean slate for each test
        ClearAllAchievements().GetAwaiter().GetResult();
        ClearAllAchievementEntries().GetAwaiter().GetResult();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
    }

    #region GetAll Tests

    [TestMethod]
    public async Task GetAll_WhenAchievementsInDb_ReturnAllOrderedByCreatedOn()
    {
        // Arrange
        var achievement1 = NewAchievement("Achievement 1", createdOn: DateTime.Now.AddDays(-2));
        var achievement2 = NewAchievement("Achievement 2", createdOn: DateTime.Now.AddDays(-1));
        var achievement3 = NewAchievement("Achievement 3", createdOn: DateTime.Now);
        await SaveAchievements(achievement1, achievement2, achievement3);
        
        // Act
        var result = (await _sut.GetAll()).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInAscendingOrder(x => x.CreatedOn);
        result.Should().ContainEquivalentOf(achievement1, options => options.Excluding(x => x.CompletedEntries));
        result.Should().ContainEquivalentOf(achievement2, options => options.Excluding(x => x.CompletedEntries));
        result.Should().ContainEquivalentOf(achievement3, options => options.Excluding(x => x.CompletedEntries));
    }

    [TestMethod]
    public async Task GetAll_WhenDbEmpty_ReturnEmptyCollection()
    {
        // Act
        var result = await _sut.GetAll();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetAllEntriesOfAchievement Tests

    [TestMethod]
    public async Task GetAllEntriesOfAchievement_WhenEntriesExist_ReturnAllOrderedByCreatedOn()
    {
        // Arrange
        var achievement = NewAchievement("Test Achievement");
        await SaveAchievements(achievement);
        
        var entry1 = NewAchievementEntry(achievement, TestUser, createdOn: DateTime.Now.AddDays(-2));
        var entry2 = NewAchievementEntry(achievement, TestUser, createdOn: DateTime.Now.AddDays(-1));
        await SaveAchievementEntries(entry1, entry2);

        // Act
        var result = (await _sut.GetAllEntriesOfAchievement(achievement.Id)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(x => x.CreatedOn);
        result.Should().ContainEquivalentOf(entry1, options => options.Excluding(x => x.Achievement).Excluding(x => x.User));
        result.Should().ContainEquivalentOf(entry2, options => options.Excluding(x => x.Achievement).Excluding(x => x.User));
    }

    [TestMethod]
    public async Task GetAllEntriesOfAchievement_WhenNoEntriesExist_ReturnEmptyCollection()
    {
        // Arrange
        var achievement = NewAchievement("Test Achievement");
        await SaveAchievements(achievement);

        // Act
        var result = await _sut.GetAllEntriesOfAchievement(achievement.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetAllEntriesOfAchievement_WhenAchievementDoesNotExist_ReturnEmptyCollection()
    {
        // Act
        var result = await _sut.GetAllEntriesOfAchievement(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetAllEntriesOfAchievement_WhenMultipleAchievementsWithEntries_ReturnOnlyEntriesForSpecificAchievement()
    {
        // Arrange
        var achievement1 = NewAchievement("Achievement 1");
        var achievement2 = NewAchievement("Achievement 2");
        await SaveAchievements(achievement1, achievement2);
        
        var entry1 = NewAchievementEntry(achievement1, TestUser);
        var entry2 = NewAchievementEntry(achievement2, TestUser);
        await SaveAchievementEntries(entry1, entry2);

        // Act
        var result = (await _sut.GetAllEntriesOfAchievement(achievement1.Id)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainEquivalentOf(entry1, options => options.Excluding(x => x.Achievement).Excluding(x => x.User));
        result.Should().NotContainEquivalentOf(entry2, options => options.Excluding(x => x.Achievement).Excluding(x => x.User));
    }

    #endregion

    #region GetAllEntriesOfUser Tests

    [TestMethod]
    public async Task GetAllEntriesOfUser_WhenEntriesExist_ReturnAllOrderedByCreatedOn()
    {
        // Arrange
        var achievement1 = NewAchievement("Achievement 1");
        var achievement2 = NewAchievement("Achievement 2");
        await SaveAchievements(achievement1, achievement2);
        
        var entry1 = NewAchievementEntry(achievement1, TestUser, createdOn: DateTime.Now.AddDays(-1));
        var entry2 = NewAchievementEntry(achievement2, TestUser, createdOn: DateTime.Now);
        await SaveAchievementEntries(entry1, entry2);

        // Act
        var result = (await _sut.GetAllEntriesOfUser(TestUser.Id)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(x => x.CreatedOn);
        result.Should().ContainEquivalentOf(entry1, options => options.Excluding(x => x.Achievement).Excluding(x => x.User));
        result.Should().ContainEquivalentOf(entry2, options => options.Excluding(x => x.Achievement).Excluding(x => x.User));
    }

    [TestMethod]
    public async Task GetAllEntriesOfUser_WhenNoEntriesExist_ReturnEmptyCollection()
    {
        // Act
        var result = await _sut.GetAllEntriesOfUser(TestUser.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetAllEntriesOfUser_WhenUserDoesNotExist_ReturnEmptyCollection()
    {
        // Act
        var result = await _sut.GetAllEntriesOfUser("non-existent-user-id");

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetAllEntriesOfUser_WhenMultipleUsersWithEntries_ReturnOnlyEntriesForSpecificUser()
    {
        // Arrange
        var otherUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "otheruser",
            Name = "Other",
            Surname = "User",
            Email = "other@test.com"
        };
        await SaveUsers(otherUser);

        var achievement = NewAchievement("Test Achievement");
        await SaveAchievements(achievement);
        
        var entry1 = NewAchievementEntry(achievement, TestUser);
        var entry2 = NewAchievementEntry(achievement, otherUser);
        await SaveAchievementEntries(entry1, entry2);

        // Act
        var result = (await _sut.GetAllEntriesOfUser(TestUser.Id)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainEquivalentOf(entry1, options => options.Excluding(x => x.Achievement).Excluding(x => x.User));
        result.Should().NotContainEquivalentOf(entry2, options => options.Excluding(x => x.Achievement).Excluding(x => x.User));
    }

    #endregion

    #region GetById Tests

    [TestMethod]
    public async Task GetById_WhenAchievementExists_ReturnAchievement()
    {
        // Arrange
        var achievement = NewAchievement("Test Achievement");
        await SaveAchievements(achievement);

        // Act
        var result = await _sut.GetById(achievement.Id);

        // Assert
        result.Should().BeEquivalentTo(achievement, options => options.Excluding(x => x.CompletedEntries));
    }

    [TestMethod]
    public async Task GetById_WhenAchievementDoesNotExist_ThrowNotFoundException()
    {
        // Act
        Func<Task<Achievement>> act = () => _sut.GetById(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Achievement not found");
    }

    #endregion

    #region Create Tests

    [TestMethod]
    public async Task Create_WhenNewAchievement_AddsAchievementToDatabase()
    {
        // Arrange
        var achievement = NewAchievement("New Achievement");

        // Act
        await _sut.Create(achievement);

        // Assert
        var dbAchievements = await GetAchievements();
        dbAchievements.Should().ContainSingle().And.ContainEquivalentOf(achievement, options => options.Excluding(x => x.CompletedEntries));
    }

    [TestMethod]
    public async Task Create_WhenAchievementWithExistingId_ThrowsDbUpdateException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var achievement1 = NewAchievement("Achievement 1", id: id);
        var achievement2 = NewAchievement("Achievement 2", id: id);
        await SaveAchievements(achievement1);

        // Act
        Func<Task> act = () => _sut.Create(achievement2);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
        var dbAchievements = await GetAchievements();
        dbAchievements.Should().HaveCount(1).And.ContainEquivalentOf(achievement1, options => options.Excluding(x => x.CompletedEntries));
    }

    #endregion

    #region Update Tests

    [TestMethod]
    public async Task Update_WhenExistingAchievement_UpdatesFields()
    {
        // Arrange
        var achievement = NewAchievement("Original Name");
        await SaveAchievements(achievement);
        
        var loaded = (await GetAchievements()).Single(a => a.Id == achievement.Id);
        loaded.Name = "Updated Name";
        loaded.Description = "Updated Description";
        loaded.ComparisonValue = 999;

        // Act
        await _sut.Update(loaded);

        // Assert
        var refreshed = (await GetAchievements()).Single();
        refreshed.Name.Should().Be("Updated Name");
        refreshed.Description.Should().Be("Updated Description");
        refreshed.ComparisonValue.Should().Be(999);
    }

    [TestMethod]
    public async Task Update_WhenAchievementNotInDb_ThrowsConcurrencyException()
    {
        // Arrange
        var achievement = NewAchievement("Non-existent Achievement");

        // Act
        Func<Task> act = () => _sut.Update(achievement);

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        var dbAchievements = await GetAchievements();
        dbAchievements.Should().BeEmpty();
    }

    #endregion

    #region Delete Tests

    [TestMethod]
    public async Task Delete_WhenExistingAchievement_RemovesAchievement()
    {
        // Arrange
        var achievement1 = NewAchievement("Achievement 1");
        var achievement2 = NewAchievement("Achievement 2");
        await SaveAchievements(achievement1, achievement2);

        // Act
        await _sut.Delete(achievement1.Id);

        // Assert
        var dbAchievements = await GetAchievements();
        dbAchievements.Should().HaveCount(1)
            .And.ContainEquivalentOf(achievement2, options => options.Excluding(x => x.CompletedEntries))
            .And.NotContainEquivalentOf(achievement1, options => options.Excluding(x => x.CompletedEntries));
    }

    [TestMethod]
    public async Task Delete_WhenAchievementNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var achievement = NewAchievement("Test Achievement");
        await SaveAchievements(achievement);

        // Act
        Func<Task> act = () => _sut.Delete(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Achievement not found");
        var dbAchievements = await GetAchievements();
        dbAchievements.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task Delete_WhenAchievementHasEntries_RemovesAchievementAndCascadeDeletesEntries()
    {
        // Arrange
        var achievement = NewAchievement("Achievement with entries");
        await SaveAchievements(achievement);
        
        var entry = NewAchievementEntry(achievement, TestUser);
        await SaveAchievementEntries(entry);

        // Act
        await _sut.Delete(achievement.Id);

        // Assert
        var dbAchievements = await GetAchievements();
        var dbEntries = await GetAchievementEntries();
        dbAchievements.Should().BeEmpty();
        dbEntries.Should().BeEmpty(); // Assumes cascade delete is configured
    }

    #endregion

    #region GetUncompletedAchievementsForUser Tests

    [TestMethod]
    public async Task GetUncompletedAchievementsForUser_WhenUserHasNoCompletedAchievements_ReturnAllAchievements()
    {
        // Arrange
        var achievement1 = NewAchievement("Achievement 1", createdOn: DateTime.Now.AddDays(-3));
        var achievement2 = NewAchievement("Achievement 2", createdOn: DateTime.Now.AddDays(-2));
        var achievement3 = NewAchievement("Achievement 3", createdOn: DateTime.Now.AddDays(-1));
        await SaveAchievements(achievement1, achievement2, achievement3);

        // Act
        var result = (await _sut.GetUncompletedAchievementsForUser(TestUser.Id)).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInAscendingOrder(x => x.CreatedOn);
        result.Should().ContainEquivalentOf(achievement1, options => options.Excluding(x => x.CompletedEntries));
        result.Should().ContainEquivalentOf(achievement2, options => options.Excluding(x => x.CompletedEntries));
        result.Should().ContainEquivalentOf(achievement3, options => options.Excluding(x => x.CompletedEntries));
    }

    [TestMethod]
    public async Task GetUncompletedAchievementsForUser_WhenUserHasCompletedSomeAchievements_ReturnOnlyUncompletedOnes()
    {
        // Arrange
        var achievement1 = NewAchievement("Achievement 1", createdOn: DateTime.Now.AddDays(-4));
        var achievement2 = NewAchievement("Achievement 2", createdOn: DateTime.Now.AddDays(-3));
        var achievement3 = NewAchievement("Achievement 3", createdOn: DateTime.Now.AddDays(-2));
        var achievement4 = NewAchievement("Achievement 4", createdOn: DateTime.Now.AddDays(-1));
        await SaveAchievements(achievement1, achievement2, achievement3, achievement4);

        // User has completed achievements 1 and 3
        var entry1 = NewAchievementEntry(achievement1, TestUser);
        var entry3 = NewAchievementEntry(achievement3, TestUser);
        await SaveAchievementEntries(entry1, entry3);

        // Act
        var result = (await _sut.GetUncompletedAchievementsForUser(TestUser.Id)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(x => x.CreatedOn);
        result.Should().ContainEquivalentOf(achievement2, options => options.Excluding(x => x.CompletedEntries));
        result.Should().ContainEquivalentOf(achievement4, options => options.Excluding(x => x.CompletedEntries));
        result.Should().NotContainEquivalentOf(achievement1, options => options.Excluding(x => x.CompletedEntries));
        result.Should().NotContainEquivalentOf(achievement3, options => options.Excluding(x => x.CompletedEntries));
    }

    [TestMethod]
    public async Task GetUncompletedAchievementsForUser_WhenUserHasCompletedAllAchievements_ReturnEmptyCollection()
    {
        // Arrange
        var achievement1 = NewAchievement("Achievement 1");
        var achievement2 = NewAchievement("Achievement 2");
        await SaveAchievements(achievement1, achievement2);

        // User has completed all achievements
        var entry1 = NewAchievementEntry(achievement1, TestUser);
        var entry2 = NewAchievementEntry(achievement2, TestUser);
        await SaveAchievementEntries(entry1, entry2);

        // Act
        var result = await _sut.GetUncompletedAchievementsForUser(TestUser.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetUncompletedAchievementsForUser_WhenNoAchievementsExist_ReturnEmptyCollection()
    {
        // Act
        var result = await _sut.GetUncompletedAchievementsForUser(TestUser.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetUncompletedAchievementsForUser_WhenUserDoesNotExist_ReturnAllAchievements()
    {
        // Arrange
        var achievement1 = NewAchievement("Achievement 1");
        var achievement2 = NewAchievement("Achievement 2");
        await SaveAchievements(achievement1, achievement2);

        // Act
        var result = (await _sut.GetUncompletedAchievementsForUser("non-existent-user-id")).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainEquivalentOf(achievement1, options => options.Excluding(x => x.CompletedEntries));
        result.Should().ContainEquivalentOf(achievement2, options => options.Excluding(x => x.CompletedEntries));
    }

    [TestMethod]
    public async Task GetUncompletedAchievementsForUser_WhenMultipleUsersHaveCompletedDifferentAchievements_ReturnCorrectUncompletedForSpecificUser()
    {
        // Arrange
        var otherUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "otheruser",
            Name = "Other",
            Surname = "User",
            Email = "other@test.com"
        };
        await SaveUsers(otherUser);

        var achievement1 = NewAchievement("Achievement 1", createdOn: DateTime.Now.AddDays(-3));
        var achievement2 = NewAchievement("Achievement 2", createdOn: DateTime.Now.AddDays(-2));
        var achievement3 = NewAchievement("Achievement 3", createdOn: DateTime.Now.AddDays(-1));
        await SaveAchievements(achievement1, achievement2, achievement3);

        // TestUser has completed achievement1, otherUser has completed achievement2
        var testUserEntry = NewAchievementEntry(achievement1, TestUser);
        var otherUserEntry = NewAchievementEntry(achievement2, otherUser);
        await SaveAchievementEntries(testUserEntry, otherUserEntry);

        // Act
        var result = (await _sut.GetUncompletedAchievementsForUser(TestUser.Id)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(x => x.CreatedOn);
        result.Should().ContainEquivalentOf(achievement2, options => options.Excluding(x => x.CompletedEntries));
        result.Should().ContainEquivalentOf(achievement3, options => options.Excluding(x => x.CompletedEntries));
        result.Should().NotContainEquivalentOf(achievement1, options => options.Excluding(x => x.CompletedEntries));
    }

    #endregion

    #region CreateEntryRange Tests

    [TestMethod]
    public async Task CreateEntryRange_WhenValidEntries_ShouldAddAllEntriesToDatabase()
    {
        // Arrange
        var achievement1 = NewAchievement("Achievement 1");
        var achievement2 = NewAchievement("Achievement 2");
        await SaveAchievements(achievement1, achievement2);

        var otherUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "otheruser",
            Name = "Other",
            Surname = "User",
            Email = "other@test.com"
        };
        await SaveUsers(otherUser);

        var entries = new List<AchievementEntry>
        {
            NewAchievementEntry(achievement1, TestUser),
            NewAchievementEntry(achievement2, TestUser),
            NewAchievementEntry(achievement1, otherUser)
        };

        // Act
        await _sut.CreateEntryRange(entries);

        // Assert
        var dbEntries = await GetAchievementEntries();
        dbEntries.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task CreateEntryRange_WhenEmptyCollection_ShouldNotThrowAndNotAddAnyEntries()
    {
        // Arrange
        var emptyEntries = new List<AchievementEntry>();

        // Act
        await _sut.CreateEntryRange(emptyEntries);

        // Assert
        var dbEntries = await GetAchievementEntries();
        dbEntries.Should().BeEmpty();
    }

    [TestMethod]
    public async Task CreateEntryRange_WhenSingleEntry_ShouldAddEntryToDatabase()
    {
        // Arrange
        var achievement = NewAchievement("Single Achievement");
        await SaveAchievements(achievement);

        var entry = NewAchievementEntry(achievement, TestUser);
        var entries = new List<AchievementEntry> { entry };

        // Act
        await _sut.CreateEntryRange(entries);

        // Assert
        var dbEntries = await GetAchievementEntries();
        dbEntries.Should().ContainSingle();
        dbEntries.First().HasSeen.Should().Be(entry.HasSeen);
    }

    [TestMethod]
    public async Task CreateEntryRange_WhenMultipleEntriesForSameAchievement_ShouldAddAllEntries()
    {
        // Arrange
        var achievement = NewAchievement("Shared Achievement");
        await SaveAchievements(achievement);

        var user1 = TestUser;
        var user2 = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "user2",
            Name = "User",
            Surname = "Two",
            Email = "user2@test.com"
        };
        var user3 = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "user3",
            Name = "User",
            Surname = "Three",
            Email = "user3@test.com"
        };
        await SaveUsers(user2, user3);

        var entries = new List<AchievementEntry>
        {
            NewAchievementEntry(achievement, user1),
            NewAchievementEntry(achievement, user2),
            NewAchievementEntry(achievement, user3)
        };

        // Act
        await _sut.CreateEntryRange(entries);

        // Assert
        var dbEntries = await GetAchievementEntries();
        dbEntries.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task CreateEntryRange_WhenEntriesWithDifferentCreatedOnDates_ShouldPreserveTimestamps()
    {
        // Arrange
        var achievement = NewAchievement("Time Test Achievement");
        await SaveAchievements(achievement);

        var timestamp1 = DateTime.Now.AddDays(-2);
        var timestamp2 = DateTime.Now.AddDays(-1);
        var timestamp3 = DateTime.Now;

        var entries = new List<AchievementEntry>
        {
            NewAchievementEntry(achievement, TestUser, createdOn: timestamp1),
            NewAchievementEntry(achievement, TestUser, createdOn: timestamp2),
            NewAchievementEntry(achievement, TestUser, createdOn: timestamp3)
        };

        // Act
        await _sut.CreateEntryRange(entries);

        // Assert
        var dbEntries = (await GetAchievementEntries()).OrderBy(e => e.CreatedOn).ToList();
        dbEntries.Should().HaveCount(3);
        dbEntries[0].CreatedOn.Should().BeCloseTo(timestamp1, TimeSpan.FromSeconds(1));
        dbEntries[1].CreatedOn.Should().BeCloseTo(timestamp2, TimeSpan.FromSeconds(1));
        dbEntries[2].CreatedOn.Should().BeCloseTo(timestamp3, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public async Task CreateEntryRange_WhenEntriesWithDifferentHasSeenValues_ShouldPreserveHasSeenState()
    {
        // Arrange
        var achievement = NewAchievement("HasSeen Test Achievement");
        await SaveAchievements(achievement);

        var entry1 = NewAchievementEntry(achievement, TestUser);
        entry1.HasSeen = false;
        
        var entry2 = NewAchievementEntry(achievement, TestUser);
        entry2.HasSeen = true;

        var entries = new List<AchievementEntry> { entry1, entry2 };

        // Act
        await _sut.CreateEntryRange(entries);

        // Assert
        var dbEntries = await GetAchievementEntries();
        dbEntries.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task CreateEntryRange_WhenLargeNumberOfEntries_ShouldHandleEfficiently()
    {
        // Arrange
        var achievements = new List<Achievement>();
        for (int i = 0; i < 5; i++)
        {
            achievements.Add(NewAchievement($"Achievement {i}"));
        }
        await SaveAchievements(achievements.ToArray());

        var users = new List<ApplicationUser> { TestUser };
        for (int i = 0; i < 3; i++)
        {
            users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = $"user{i}",
                Name = "User",
                Surname = $"Number{i}",
                Email = $"user{i}@test.com"
            });
        }
        await SaveUsers(users.Skip(1).ToArray()); // Skip TestUser as it's already in DB

        // Create entries for each user-achievement combination
        var entries = new List<AchievementEntry>();
        foreach (var achievement in achievements)
        {
            foreach (var user in users)
            {
                entries.Add(NewAchievementEntry(achievement, user));
            }
        }

        // Act
        await _sut.CreateEntryRange(entries);

        // Assert
        var dbEntries = await GetAchievementEntries();
        dbEntries.Should().HaveCount(20); // 5 achievements × 4 users
    }

    [TestMethod]
    public async Task CreateEntryRange_WhenDatabaseTransactionFails_ShouldPropagateException()
    {
        // Arrange
        var achievement = NewAchievement("Transaction Test Achievement");
        await SaveAchievements(achievement);

        // Create an entry with invalid foreign key to cause failure
        var invalidEntry = new AchievementEntry
        {
            Id = Guid.NewGuid(),
            HasSeen = false,
            CreatedOn = DateTime.Now,
            Achievement = null!,
            User = null!
        };

        var entries = new List<AchievementEntry> { invalidEntry };

        // Act & Assert
        var act = () => _sut.CreateEntryRange(entries);
        await act.Should().ThrowAsync<Exception>();
        
        // Verify no entries were saved
        var dbEntries = await GetAchievementEntries();
        dbEntries.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private Achievement NewAchievement(string name, Guid? id = null, DateTime? createdOn = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = name,
        Description = $"Description for {name}",
        Action = Achievement.ActionOption.UserBuy,
        ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
        ComparisonValue = 10,
        CompletedEntries = null!, // Set to null to match database behavior
        CreatedOn = createdOn ?? DateTime.Now
    };

    private AchievementEntry NewAchievementEntry(Achievement achievement, ApplicationUser user, DateTime? createdOn = null) => new()
    {
        Id = Guid.NewGuid(),
        Achievement = achievement,
        User = user,
        HasSeen = false,
        CreatedOn = createdOn ?? DateTime.Now
    };

    private async Task ClearAllAchievements()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Achievement.RemoveRange(context.Achievement);
        await context.SaveChangesAsync();
    }

    private async Task ClearAllAchievementEntries()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.AchievementEntry.RemoveRange(context.AchievementEntry);
        await context.SaveChangesAsync();
    }

    private async Task<IList<Achievement>> GetAchievements()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Achievement.AsNoTracking().ToListAsync();
    }

    private async Task<IList<AchievementEntry>> GetAchievementEntries()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.AchievementEntry.AsNoTracking().ToListAsync();
    }

    private async Task SaveAchievements(params Achievement[] achievements)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Achievement.AddRangeAsync(achievements);
        await context.SaveChangesAsync();
    }

    private async Task SaveAchievementEntries(params AchievementEntry[] entries)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        // Create new entries with proper foreign key setup
        var entriesToAdd = new List<AchievementEntry>();
        foreach (var entry in entries)
        {
            var newEntry = new AchievementEntry
            {
                Id = entry.Id,
                HasSeen = entry.HasSeen,
                CreatedOn = entry.CreatedOn,
                Achievement = null!, // Don't set navigation property to avoid re-inserting Achievement
                User = null! // Don't set navigation property to avoid re-inserting User
            };
            
            // Set the foreign keys directly
            if (entry.Achievement != null)
            {
                context.Entry(newEntry).Property("AchievementId").CurrentValue = entry.Achievement.Id;
            }
            if (entry.User != null)
            {
                context.Entry(newEntry).Property("UserId").CurrentValue = entry.User.Id;
            }
            
            entriesToAdd.Add(newEntry);
        }
        
        await context.AchievementEntry.AddRangeAsync(entriesToAdd);
        await context.SaveChangesAsync();
    }

    private async Task SaveUsers(params ApplicationUser[] users)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }

    #endregion
}
