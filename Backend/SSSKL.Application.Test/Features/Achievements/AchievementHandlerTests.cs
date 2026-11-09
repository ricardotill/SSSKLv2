using System.Net.Mime;
using FluentAssertions;
using NSubstitute;
using SSSKL.Application.Features.Achievements;
using SSSKL.Application.Features.Achievements.Commands.CreateAchievement;
using SSSKL.Application.Features.Achievements.Commands.DeleteAchievement;
using SSSKL.Application.Features.Achievements.Commands.DeleteAchievementEntries;
using SSSKL.Application.Features.Achievements.Commands.MarkUnseenEntriesSeen;
using SSSKL.Application.Features.Achievements.Commands.UpdateAchievement;
using SSSKL.Application.Features.Achievements.Queries.GetAchievementById;
using SSSKL.Application.Features.Achievements.Queries.GetAchievementEarners;
using SSSKL.Application.Features.Achievements.Queries.GetAchievementEntries;
using SSSKL.Application.Features.Achievements.Queries.GetAchievementListing;
using SSSKL.Application.Features.Achievements.Queries.GetAchievements;
using SSSKL.Contracts.Agents;
using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Test.Features.Achievements;

[TestClass]
public class AchievementHandlerTests
{
    private IAchievementRepository _achievementRepository = null!;
    private IApplicationUserRepository _applicationUserRepository = null!;
    private IBlobStorageAgent _blobStorageAgent = null!;
    private ApplicationUser _testUser = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _achievementRepository = Substitute.For<IAchievementRepository>();
        _applicationUserRepository = Substitute.For<IApplicationUserRepository>();
        _blobStorageAgent = Substitute.For<IBlobStorageAgent>();
        _testUser = new ApplicationUser { Id = "test-user-id", UserName = "testuser", Name = "Test", Surname = "User" };
    }

    #region Listing & entries queries

    [TestMethod]
    public async Task GetListing_ByUsername_UserExists_ReturnsListWithCorrectCompletedFlags()
    {
        // Arrange
        var a1 = new Achievement { Id = Guid.NewGuid(), Name = "A1" };
        var a2 = new Achievement { Id = Guid.NewGuid(), Name = "A2" };
        _applicationUserRepository.GetByUsername("testuser").Returns(_testUser);
        _achievementRepository.GetAll().Returns(new List<Achievement> { a1, a2 });
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(new List<AchievementEntry>
        {
            new AchievementEntry { Achievement = a1, User = _testUser }
        });
        var sut = new GetAchievementListingHandler(_achievementRepository, _applicationUserRepository);

        // Act
        var result = await sut.Handle(new GetAchievementListingQuery(Username: "testuser"), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Single(a => a.Name == "A1").Completed.Should().BeTrue();
        result.Single(a => a.Name == "A2").Completed.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetListing_ByUsername_UserNotFound_ReturnsEmptyList()
    {
        _applicationUserRepository.GetByUsername("nonexistent").Returns(Task.FromResult<ApplicationUser>(null!));
        var sut = new GetAchievementListingHandler(_achievementRepository, _applicationUserRepository);

        var result = await sut.Handle(new GetAchievementListingQuery(Username: "nonexistent"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetListing_ByUserId_DoesNotResolveUsername()
    {
        _achievementRepository.GetAll().Returns(new List<Achievement>());
        _achievementRepository.GetAllEntriesOfUser("u1").Returns(new List<AchievementEntry>());
        var sut = new GetAchievementListingHandler(_achievementRepository, _applicationUserRepository);

        var result = await sut.Handle(new GetAchievementListingQuery(UserId: "u1"), CancellationToken.None);

        result.Should().BeEmpty();
        await _achievementRepository.Received(1).GetAllEntriesOfUser("u1");
        await _applicationUserRepository.DidNotReceive().GetByUsername(Arg.Any<string>());
    }

    [TestMethod]
    public async Task GetEntries_ByUsername_UserExists_ReturnsEntries()
    {
        var entry = new AchievementEntry { Id = Guid.NewGuid(), User = _testUser, Achievement = new Achievement { Id = Guid.NewGuid(), Name = "A" } };
        _applicationUserRepository.GetByUsername("testuser").Returns(_testUser);
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(new List<AchievementEntry> { entry });
        var sut = new GetAchievementEntriesHandler(_achievementRepository, _applicationUserRepository);

        var result = await sut.Handle(new GetAchievementEntriesQuery(Username: "testuser"), CancellationToken.None);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(entry.ToDto());
    }

    [TestMethod]
    public async Task GetEntries_ByUsername_UserNotFound_ReturnsEmptyList()
    {
        _applicationUserRepository.GetByUsername("no-user").Returns(Task.FromResult<ApplicationUser>(null!));
        var sut = new GetAchievementEntriesHandler(_achievementRepository, _applicationUserRepository);

        var result = await sut.Handle(new GetAchievementEntriesQuery(Username: "no-user"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetEarners_ReturnsEntriesFromRepository()
    {
        var achievementId = Guid.NewGuid();
        var entries = new List<AchievementEntry> { new AchievementEntry { Id = Guid.NewGuid() } };
        _achievementRepository.GetAllEntries(achievementId).Returns(entries);
        var sut = new GetAchievementEarnersHandler(_achievementRepository);

        var result = await sut.Handle(new GetAchievementEarnersQuery(achievementId), CancellationToken.None);

        result.Should().ContainSingle().Which.Id.Should().Be(entries[0].Id);
    }

    #endregion

    #region Admin queries

    [TestMethod]
    public async Task GetAchievements_Paged_ShouldReturnPagedResultsAndCount()
    {
        var achievements = new List<Achievement> { new Achievement { Id = Guid.NewGuid(), Name = "A" } };
        _achievementRepository.GetAll(10, 5).Returns(Task.FromResult<IList<Achievement>>(achievements));
        _achievementRepository.GetCount().Returns(5);
        var sut = new GetAchievementsHandler(_achievementRepository);

        var result = await sut.Handle(new GetAchievementsQuery(10, 5), CancellationToken.None);

        result.Items.Should().ContainSingle().Which.Id.Should().Be(achievements[0].Id);
        result.TotalCount.Should().Be(5);
    }

    [TestMethod]
    public async Task GetAchievements_NoAchievements_ReturnsEmpty()
    {
        _achievementRepository.GetAll(0, 15).Returns(Task.FromResult<IList<Achievement>>(new List<Achievement>()));
        var sut = new GetAchievementsHandler(_achievementRepository);

        var result = await sut.Handle(new GetAchievementsQuery(0, 15), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetAchievementById_ShouldReturnMappedAchievement()
    {
        var id = Guid.NewGuid();
        var achievement = new Achievement { Id = id, Name = "A", Tier = Achievement.AchievementTier.Gold };
        _achievementRepository.GetById(id).Returns(achievement);
        var sut = new GetAchievementByIdHandler(_achievementRepository);

        var result = await sut.Handle(new GetAchievementByIdQuery(id), CancellationToken.None);

        result.Id.Should().Be(id);
        result.Tier.Should().Be("Gold");
    }

    #endregion

    #region Commands

    [TestMethod]
    public async Task MarkUnseenEntriesSeen_WhenUnseenExist_MarksAsSeenAndReturnsUpdated()
    {
        var entries = new List<AchievementEntry>
        {
            new AchievementEntry { Id = Guid.NewGuid(), Achievement = new Achievement { Id = Guid.NewGuid() }, HasSeen = false }
        };
        _achievementRepository.GetPersonalUnseenAchievementEntries("user1").Returns(entries);
        var sut = new MarkUnseenEntriesSeenHandler(_achievementRepository);

        var result = await sut.Handle(new MarkUnseenEntriesSeenCommand("user1"), CancellationToken.None);

        result.Should().ContainSingle().Which.HasSeen.Should().BeTrue();
        await _achievementRepository.Received(1).UpdateAchievementEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(list => list.All(e => e.HasSeen)));
    }

    [TestMethod]
    public async Task MarkUnseenEntriesSeen_WhenNoneUnseen_DoesNotUpdate()
    {
        _achievementRepository.GetPersonalUnseenAchievementEntries("user1").Returns(new List<AchievementEntry>());
        var sut = new MarkUnseenEntriesSeenHandler(_achievementRepository);

        var result = await sut.Handle(new MarkUnseenEntriesSeenCommand("user1"), CancellationToken.None);

        result.Should().BeEmpty();
        await _achievementRepository.DidNotReceive().UpdateAchievementEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }

    [TestMethod]
    public async Task UpdateAchievement_WithoutImage_KeepsExistingImage()
    {
        var id = Guid.NewGuid();
        var image = new AchievementImage { Id = Guid.NewGuid(), FileName = "keep.png", Uri = "u", ContentType = "image/png" };
        _achievementRepository.GetById(id).Returns(new Achievement { Id = id, Image = image });
        var sut = new UpdateAchievementHandler(_achievementRepository, _blobStorageAgent);

        await sut.Handle(new UpdateAchievementCommand(new AchievementUpdateDto { Id = id, Name = "New", Tier = "Silver" }), CancellationToken.None);

        await _achievementRepository.Received(1).Update(Arg.Is<Achievement>(a =>
            a.Id == id && a.Name == "New" && a.Tier == Achievement.AchievementTier.Silver && a.Image == image));
    }

    [TestMethod]
    public async Task UpdateAchievement_WithImageDto_UsesProvidedImage()
    {
        var id = Guid.NewGuid();
        var imageDto = new AchievementImageDto { Id = Guid.NewGuid(), FileName = "given.png", Uri = "u", ContentType = "image/png" };
        var sut = new UpdateAchievementHandler(_achievementRepository, _blobStorageAgent);

        await sut.Handle(new UpdateAchievementCommand(new AchievementUpdateDto { Id = id, Name = "N", Image = imageDto }), CancellationToken.None);

        await _achievementRepository.Received(1).Update(Arg.Is<Achievement>(a => a.Image != null && a.Image.FileName == "given.png"));
        await _achievementRepository.DidNotReceive().GetById(Arg.Any<Guid>());
    }

    [TestMethod]
    public async Task UpdateAchievement_WithNewImage_UploadsAndDeletesOldBlob()
    {
        var id = Guid.NewGuid();
        var existing = new Achievement
        {
            Id = id,
            Name = "Old",
            Image = new AchievementImage { Id = Guid.NewGuid(), FileName = "old.png", Uri = "http://test.com/old.png", ContentType = "image/png" }
        };
        _achievementRepository.GetById(id).Returns(Task.FromResult(existing));

        var blobItem = new BlobStorageItem
        {
            Id = Guid.NewGuid(),
            FileName = "new.png",
            Uri = "http://test.com/new.png",
            ContentType = "image/png",
            CreatedOn = DateTime.Now
        };
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _blobStorageAgent.UploadFileToBlobAsync(Arg.Any<string>(), "image/png", stream).Returns(blobItem);
        var sut = new UpdateAchievementHandler(_achievementRepository, _blobStorageAgent);

        await sut.Handle(new UpdateAchievementCommand(new AchievementUpdateDto { Id = id, Name = "Old" }, stream, "image/png"), CancellationToken.None);

        await _achievementRepository.Received(1).Update(Arg.Is<Achievement>(a => a.Image != null && a.Image.FileName == "new.png"));
        await _blobStorageAgent.Received(1).DeleteFileToBlobAsync("old.png");
    }

    [TestMethod]
    public async Task UpdateAchievement_WithUnsupportedNewImageContentType_ShouldThrowArgumentException()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var sut = new UpdateAchievementHandler(_achievementRepository, _blobStorageAgent);

        var act = () => sut.Handle(new UpdateAchievementCommand(new AchievementUpdateDto { Id = Guid.NewGuid(), Name = "Old" }, stream, "application/pdf"), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task DeleteAchievement_ShouldCallRepository()
    {
        var id = Guid.NewGuid();
        await new DeleteAchievementHandler(_achievementRepository).Handle(new DeleteAchievementCommand(id), CancellationToken.None);
        await _achievementRepository.Received(1).Delete(id);
    }

    [TestMethod]
    public async Task DeleteAchievementEntries_ShouldCallRepositoryWithEntryIds()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        await new DeleteAchievementEntriesHandler(_achievementRepository).Handle(new DeleteAchievementEntriesCommand(ids), CancellationToken.None);

        await _achievementRepository.Received(1).DeleteAchievementEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(e => e.Select(x => x.Id).SequenceEqual(ids)));
    }

    [TestMethod]
    public async Task CreateAchievement_WithValidData_ShouldUploadImageAndCreateAchievement()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 0x01, 0x02 });
        var dto = new AchievementDto
        {
            Name = "New Achievement",
            Description = "Desc",
            ImageContent = stream,
            ImageContentType = new ContentType("image/png"),
            AutoAchieve = true,
            Action = Achievement.ActionOption.UserOrderAmountBought,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 10
        };
        var blobItem = new BlobStorageItem
        {
            Id = Guid.NewGuid(),
            FileName = "test.png",
            Uri = "http://test.com/test.png",
            ContentType = "image/png",
            CreatedOn = DateTime.Now
        };
        _blobStorageAgent.UploadFileToBlobAsync(Arg.Any<string>(), "image/png", stream).Returns(blobItem);
        var sut = new CreateAchievementHandler(_achievementRepository, _blobStorageAgent);

        // Act
        await sut.Handle(new CreateAchievementCommand(dto), CancellationToken.None);

        // Assert
        await _blobStorageAgent.Received(1).UploadFileToBlobAsync(Arg.Any<string>(), "image/png", stream);
        await _achievementRepository.Received(1).Create(Arg.Is<Achievement>(a =>
            a.Name == dto.Name &&
            a.Description == dto.Description &&
            a.Image != null &&
            a.Image.FileName == "test.png"));
    }

    [TestMethod]
    public async Task CreateAchievement_MissingImage_ShouldThrowArgumentException()
    {
        var sut = new CreateAchievementHandler(_achievementRepository, _blobStorageAgent);

        var act = () => sut.Handle(new CreateAchievementCommand(new AchievementDto { Name = "Fail", Description = "Fail" }), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
