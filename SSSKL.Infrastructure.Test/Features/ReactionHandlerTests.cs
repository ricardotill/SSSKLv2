using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using SSSKL.Application.Features.Reactions.Commands.DeleteReaction;
using SSSKL.Application.Features.Reactions.Commands.ToggleReaction;
using SSSKL.Application.Features.Reactions.Queries.GetReactions;
using SSSKL.Application.Features.Reactions.Queries.GetReactionTimeline;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Events;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Infrastructure.Persistence;
using SSSKL.Infrastructure.Persistence.Repositories;
using SSSKL.Infrastructure.Test.Util;

namespace SSSKL.Infrastructure.Test.Features;

[TestClass]
public class ReactionHandlerTests : RepositoryTest
{
    private ApplicationDbContext _dbContext = null!;
    private ReactionRepository _reactionRepository = null!;
    private EventRepository _eventRepository = null!;
    private ApplicationUserRepository _applicationUserRepository = null!;
    private IUserRoleProvider _roleProvider = null!;
    private IUserNotifier _userNotifier = null!;
    private IDomainEventDispatcher _mockEventDispatcher = null!;
    private IUserStatRepository _mockUserStatRepository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContext = new ApplicationDbContext(GetOptions());
        _roleProvider = Substitute.For<IUserRoleProvider>();
        _userNotifier = Substitute.For<IUserNotifier>();
        _mockEventDispatcher = Substitute.For<IDomainEventDispatcher>();
        _mockUserStatRepository = Substitute.For<IUserStatRepository>();
        _mockUserStatRepository.RecalculateByUserId(Arg.Any<string>()).Returns(call => new UserStat { UserId = call.Arg<string>() });
        var factory = new MockDbContextFactory(GetOptions());
        _reactionRepository = new ReactionRepository(_dbContext);
        _eventRepository = new EventRepository(factory);
        _applicationUserRepository = new ApplicationUserRepository(factory);
    }

    // The tests below were written against ReactionService; these helpers send the same calls to the handlers.

    private Task ToggleReaction(Guid targetId, string targetType, string content, string userId) =>
        new ToggleReactionHandler(_reactionRepository, _eventRepository, _applicationUserRepository, _roleProvider, _userNotifier, _mockEventDispatcher, _mockUserStatRepository)
            .Handle(new ToggleReactionCommand(targetId, targetType, content, userId), CancellationToken.None);

    private Task<IReadOnlyList<ReactionDto>> GetReactionsForTarget(Guid targetId, string targetType) =>
        new GetReactionsHandler(_reactionRepository).Handle(new GetReactionsQuery(targetId, targetType), CancellationToken.None);

    private Task<IReadOnlyList<ReactionDto>> GetTimeline(int skip, int take) =>
        new GetReactionTimelineHandler(_reactionRepository).Handle(new GetReactionTimelineQuery(skip, take), CancellationToken.None);

    private Task DeleteReaction(Guid id, string userId, bool isAdmin) =>
        new DeleteReactionHandler(_reactionRepository, _mockUserStatRepository).Handle(new DeleteReactionCommand(id, userId, isAdmin), CancellationToken.None);

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task ToggleReaction_NewReaction_ShouldAdd()
    {
        // Arrange
        var evt = await CreateTestEvent();
        var targetId = evt.Id;
        var targetType = "Event";
        var content = "👍";
        var userId = TestUser.Id;

        // Act
        await ToggleReaction(targetId, targetType, content, userId);

        // Assert
        var reaction = await _dbContext.Reaction.FirstOrDefaultAsync(r => r.TargetId == targetId && r.Content == content);
        reaction.Should().NotBeNull();
        reaction!.UserId.Should().Be(userId);
        reaction.TargetType.Should().Be(ReactionTargetType.Event);
    }

    [TestMethod]
    public async Task ToggleReaction_ExistingReaction_ShouldRemove()
    {
        // Arrange
        var evt = await CreateTestEvent();
        var targetId = evt.Id;
        var targetType = ReactionTargetType.Event;
        var content = "❤️";
        var userId = TestUser.Id;

        var existing = new Reaction
        {
            TargetId = targetId,
            TargetType = targetType,
            UserId = userId,
            Content = content,
            CreatedOn = DateTime.UtcNow
        };
        _dbContext.Reaction.Add(existing);
        await _dbContext.SaveChangesAsync();

        // Act
        await ToggleReaction(targetId, targetType.ToString(), content, userId);

        // Assert
        var reaction = await _dbContext.Reaction.FirstOrDefaultAsync(r => r.TargetId == targetId && r.Content == content);
        reaction.Should().BeNull();
    }

    [TestMethod]
    public async Task ToggleReaction_DifferentContent_ShouldAddSecondReaction()
    {
        // Arrange
        var evt = await CreateTestEvent();
        var targetId = evt.Id;
        var userId = TestUser.Id;
        
        // Add first reaction
        await ToggleReaction(targetId, "Event", "👍", userId);

        // Act - Toggle different content
        await ToggleReaction(targetId, "Event", "❤️", userId);

        // Assert
        var reactions = await _dbContext.Reaction.Where(r => r.TargetId == targetId).ToListAsync();
        reactions.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task ToggleReaction_InvalidType_ShouldThrow()
    {
        // Arrange
        var targetId = Guid.NewGuid();

        // Act & Assert
        await this.Invoking(s => s.ToggleReaction(targetId, "InvalidType", "👍", TestUser.Id))
            .Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task GetReactionsForTarget_ShouldReturnMappedDtos()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var reaction = new Reaction
        {
            TargetId = targetId,
            TargetType = ReactionTargetType.Event,
            UserId = TestUser.Id,
            Content = "🔥",
            CreatedOn = DateTime.UtcNow
        };
        _dbContext.Reaction.Add(reaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await GetReactionsForTarget(targetId, "Event");

        // Assert
        result.Should().HaveCount(1);
        var dto = result.First();
        dto.Content.Should().Be("🔥");
        dto.UserName.Should().Be(TestUser.UserName);
        dto.TargetId.Should().Be(targetId);
    }

    [TestMethod]
    public async Task GetTimeline_ShouldReturnRecentReactions()
    {
        // Arrange
        var r1 = new Reaction { TargetId = Guid.NewGuid(), TargetType = ReactionTargetType.Event, UserId = TestUser.Id, Content = "1", CreatedOn = DateTime.UtcNow.AddMinutes(-10) };
        var r2 = new Reaction { TargetId = Guid.NewGuid(), TargetType = ReactionTargetType.Event, UserId = TestUser.Id, Content = "2", CreatedOn = DateTime.UtcNow.AddMinutes(-5) };
        _dbContext.Reaction.AddRange(r1, r2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await GetTimeline(0, 10);

        // Assert
        result.Should().HaveCount(2);
        result.First().Content.Should().Be("2"); // Newest first
    }

    [TestMethod]
    public async Task ToggleReaction_OnEvent_ShouldNotifyCreator()
    {
        // Arrange
        var creatorId = Guid.NewGuid().ToString();
        await CreateUser(creatorId);
        
        var evt = new Event 
        { 
            Id = Guid.NewGuid(), 
            Title = "Party", 
            Description = "D", 
            CreatorId = creatorId,
            StartDateTime = DateTime.UtcNow,
            EndDateTime = DateTime.UtcNow.AddHours(2)
        };
        _dbContext.Event.Add(evt);
        await _dbContext.SaveChangesAsync();

        // Act
        await ToggleReaction(evt.Id, "Event", "👍", TestUser.Id);

        // Assert
        await _userNotifier.Received(1).NotifyAsync(
            creatorId,
            "Nieuwe reactie",
            Arg.Is<string>(s => s.Contains("Party")),
            Arg.Is<string>(s => s.Contains(evt.Id.ToString())),
            true
        );
    }

    [TestMethod]
    public async Task ToggleReaction_OnReaction_ShouldNotifyAuthor()
    {
        // Arrange
        var evt = await CreateTestEvent();
        var authorId = Guid.NewGuid().ToString();
        await CreateUser(authorId);

        var rootReaction = new Reaction 
        { 
            Id = Guid.NewGuid(), 
            TargetId = evt.Id, 
            TargetType = ReactionTargetType.Event, 
            UserId = authorId, 
            Content = "Nice!" 
        };
        _dbContext.Reaction.Add(rootReaction);
        await _dbContext.SaveChangesAsync();

        // Act
        await ToggleReaction(rootReaction.Id, "Reaction", "Reply", TestUser.Id);

        // Assert
        await _userNotifier.Received(1).NotifyAsync(
            authorId,
            "Nieuw antwoord",
            Arg.Any<string>(),
            Arg.Any<string>(),
            true
        );
    }

    [TestMethod]
    public async Task ToggleReaction_ByOwner_ShouldNotNotify()
    {
        // Arrange
        var evt = await CreateTestEvent(); // Creator is TestUser.Id

        // Act
        await ToggleReaction(evt.Id, "Event", "👍", TestUser.Id);

        // Assert
        await _userNotifier.DidNotReceive().NotifyAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    [TestMethod]
    public async Task ToggleReaction_ReactionOnReaction_ShouldWork()
    {
        // Arrange
        var evt = await CreateTestEvent();
        var originalTargetId = evt.Id;
        var reaction1 = new Reaction
        {
            Id = Guid.NewGuid(),
            TargetId = originalTargetId,
            TargetType = ReactionTargetType.Event,
            UserId = TestUser.Id,
            Content = "Original",
            CreatedOn = DateTime.UtcNow
        };
        _dbContext.Reaction.Add(reaction1);
        await _dbContext.SaveChangesAsync();
        // Act - React to the reaction
        await ToggleReaction(reaction1.Id, "Reaction", "Reply", TestUser.Id);

        // Assert
        var result = await _dbContext.Reaction.FirstOrDefaultAsync(r => r.TargetId == reaction1.Id && r.TargetType == ReactionTargetType.Reaction);
        result.Should().NotBeNull();
        result!.Content.Should().Be("Reply");
    }

    [TestMethod]
    public async Task GetReactionsForTarget_WithMultipleLevels_ShouldReturnFlattenedRepliesWithTargetNames()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var l1Id = Guid.NewGuid();
        var l2Id = Guid.NewGuid();
        
        var level1 = new Reaction { Id = l1Id, TargetId = eventId, TargetType = ReactionTargetType.Event, UserId = TestUser.Id, Content = "Level 1", CreatedOn = DateTime.UtcNow };
        var level2 = new Reaction { Id = l2Id, TargetId = l1Id, TargetType = ReactionTargetType.Reaction, UserId = TestUser.Id, Content = "Level 2", CreatedOn = DateTime.UtcNow.AddSeconds(1) };
        var level3 = new Reaction { Id = Guid.NewGuid(), TargetId = l2Id, TargetType = ReactionTargetType.Reaction, UserId = TestUser.Id, Content = "Level 3", CreatedOn = DateTime.UtcNow.AddSeconds(2) };
        
        _dbContext.Reaction.AddRange(level1, level2, level3);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await GetReactionsForTarget(eventId, "Event");

        // Assert
        var topLevel = result.ToList();
        topLevel.Should().HaveCount(1);
        
        var parentDto = topLevel.First();
        parentDto.Reactions.Should().HaveCount(2); // Level 2 and Level 3 should be flattened under parent
        
        var reply2 = parentDto.Reactions.First(r => r.Content == "Level 2");
        var reply3 = parentDto.Reactions.First(r => r.Content == "Level 3");
        
        reply2.TargetUserName.Should().Be(TestUser.UserName);
        reply3.TargetUserName.Should().Be(TestUser.UserName);
        reply3.TargetId.Should().Be(l2Id);
    }

    private async Task<Event> CreateTestEvent()
    {
        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            Description = "Description",
            CreatorId = TestUser.Id,
            StartDateTime = DateTime.UtcNow,
            EndDateTime = DateTime.UtcNow.AddHours(1)
        };
        _dbContext.Event.Add(evt);
        await _dbContext.SaveChangesAsync();
        return evt;
    }

    private async Task CreateUser(string id)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = "user-" + id,
            Name = "Test",
            Surname = "User",
            Email = "test@test.com"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
    }

    [TestMethod]
    public async Task ToggleReaction_UserMissingRequiredRole_ShouldThrowUnauthorized()
    {
        // Arrange
        var role = new IdentityRole { Name = "SpecialRole" };
        _dbContext.Roles.Add(role);
        
        var evt = new Event 
        { 
            Id = Guid.NewGuid(), 
            Title = "Restricted", 
            Description = "desc", 
            CreatorId = TestUser.Id,
            RequiredRoles = new List<IdentityRole> { role }
        };
        _dbContext.Event.Add(evt);
        await _dbContext.SaveChangesAsync();

        _roleProvider.GetRoles(TestUser.Id).Returns(new List<string> { Roles.User });

        // Act & Assert
        await this.Invoking(s => s.ToggleReaction(evt.Id, "Event", "👍", TestUser.Id))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task ToggleReaction_UserHasRequiredRole_ShouldSucceed()
    {
        // Arrange
        var roleName = "SpecialRole";
        var role = new IdentityRole { Name = roleName };
        _dbContext.Roles.Add(role);
        
        var evt = new Event 
        { 
            Id = Guid.NewGuid(), 
            Title = "Restricted", 
            Description = "desc", 
            CreatorId = TestUser.Id,
            RequiredRoles = new List<IdentityRole> { role }
        };
        _dbContext.Event.Add(evt);
        await _dbContext.SaveChangesAsync();

        _roleProvider.GetRoles(TestUser.Id).Returns(new List<string> { roleName });

        // Act
        await ToggleReaction(evt.Id, "Event", "👍", TestUser.Id);

        // Assert
        var reaction = await _dbContext.Reaction.FirstOrDefaultAsync(r => r.TargetId == evt.Id);
        reaction.Should().NotBeNull();
    }

    [TestMethod]
    public async Task ToggleReaction_Admin_ShouldSucceedEvenWithoutRole()
    {
        // Arrange
        var role = new IdentityRole { Name = "SpecialRole" };
        _dbContext.Roles.Add(role);
        
        var evt = new Event 
        { 
            Id = Guid.NewGuid(), 
            Title = "Restricted", 
            Description = "desc", 
            CreatorId = TestUser.Id,
            RequiredRoles = new List<IdentityRole> { role }
        };
        _dbContext.Event.Add(evt);
        await _dbContext.SaveChangesAsync();

        _roleProvider.GetRoles(TestUser.Id).Returns(new List<string> { Roles.Admin });

        // Act
        await ToggleReaction(evt.Id, "Event", "👍", TestUser.Id);

        // Assert
        var reaction = await _dbContext.Reaction.FirstOrDefaultAsync(r => r.TargetId == evt.Id);
        reaction.Should().NotBeNull();
    }

    [TestMethod]
    public async Task ToggleReaction_NestedReaction_MissingRoleOnRoot_ShouldThrow()
    {
        // Arrange
        var role = new IdentityRole { Name = "SpecialRole" };
        _dbContext.Roles.Add(role);
        
        var evt = new Event { Id = Guid.NewGuid(), Title = "Restricted", Description = "desc", CreatorId = TestUser.Id, RequiredRoles = new List<IdentityRole> { role } };
        var rootReaction = new Reaction { Id = Guid.NewGuid(), TargetId = evt.Id, TargetType = ReactionTargetType.Event, UserId = TestUser.Id, Content = "Root" };
        
        _dbContext.Event.Add(evt);
        _dbContext.Reaction.Add(rootReaction);
        await _dbContext.SaveChangesAsync();

        _roleProvider.GetRoles(TestUser.Id).Returns(new List<string> { Roles.User });

        // Act & Assert
        await this.Invoking(s => s.ToggleReaction(rootReaction.Id, "Reaction", "Reply", TestUser.Id))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task DeleteReaction_AsOwner_ShouldSucceed()
    {
        // Arrange
        var evt = await CreateTestEvent();
        var reaction = new Reaction { TargetId = evt.Id, TargetType = ReactionTargetType.Event, UserId = TestUser.Id, Content = "To Delete" };
        _dbContext.Reaction.Add(reaction);
        await _dbContext.SaveChangesAsync();

        // Act
        await DeleteReaction(reaction.Id, TestUser.Id, false);

        // Assert
        var result = await _dbContext.Reaction.FindAsync(reaction.Id);
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task DeleteReaction_AsAdmin_ShouldSucceed()
    {
        // Arrange
        var evt = await CreateTestEvent();
        var otherUserId = Guid.NewGuid().ToString();
        await CreateUser(otherUserId);
        var reaction = new Reaction { TargetId = evt.Id, TargetType = ReactionTargetType.Event, UserId = otherUserId, Content = "To Delete" };
        _dbContext.Reaction.Add(reaction);
        await _dbContext.SaveChangesAsync();

        // Act
        await DeleteReaction(reaction.Id, "admin-id", true);

        // Assert
        var result = await _dbContext.Reaction.FindAsync(reaction.Id);
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task DeleteReaction_AsNonOwner_ShouldThrow()
    {
        // Arrange
        var evt = await CreateTestEvent();
        var otherUserId = Guid.NewGuid().ToString();
        await CreateUser(otherUserId);
        var reaction = new Reaction { TargetId = evt.Id, TargetType = ReactionTargetType.Event, UserId = otherUserId, Content = "To Delete" };
        _dbContext.Reaction.Add(reaction);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await this.Invoking(s => s.DeleteReaction(reaction.Id, TestUser.Id, false))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task DeleteReaction_Recursive_ShouldDeleteAllChildren()
    {
        // Arrange
        var evt = await CreateTestEvent();
        var l1 = new Reaction { Id = Guid.NewGuid(), TargetId = evt.Id, TargetType = ReactionTargetType.Event, UserId = TestUser.Id, Content = "L1" };
        var l2 = new Reaction { Id = Guid.NewGuid(), TargetId = l1.Id, TargetType = ReactionTargetType.Reaction, UserId = TestUser.Id, Content = "L2" };
        var l3 = new Reaction { Id = Guid.NewGuid(), TargetId = l2.Id, TargetType = ReactionTargetType.Reaction, UserId = TestUser.Id, Content = "L3" };
        
        _dbContext.Reaction.AddRange(l1, l2, l3);
        await _dbContext.SaveChangesAsync();

        // Act
        await DeleteReaction(l1.Id, TestUser.Id, false);

        // Assert
        _dbContext.Reaction.Any(r => r.Id == l1.Id).Should().BeFalse();
        _dbContext.Reaction.Any(r => r.Id == l2.Id).Should().BeFalse();
        _dbContext.Reaction.Any(r => r.Id == l3.Id).Should().BeFalse();
    }
}
