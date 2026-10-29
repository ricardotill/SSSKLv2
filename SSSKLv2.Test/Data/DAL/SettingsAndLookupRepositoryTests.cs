using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class SettingsAndLookupRepositoryTests : RepositoryTest
{
    private ApplicationDbContext _context = null!;

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void Init()
    {
        InitializeDatabase();
        _context = new ApplicationDbContext(GetOptions());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
        CleanupDatabase();
    }

    [TestMethod]
    public async Task GlobalSettingRepository_Upsert_CreatesThenUpdates()
    {
        var sut = new GlobalSettingRepository(_context);

        (await sut.GetByKey("Key")).Should().BeNull();

        await sut.Upsert("Key", "first");
        (await sut.GetByKey("Key"))!.Value.Should().Be("first");

        await sut.Upsert("Key", "second");
        var setting = await sut.GetByKey("Key");
        setting!.Value.Should().Be("second");
        (await _context.GlobalSetting.CountAsync(TestContext.CancellationToken)).Should().Be(1);
    }

    [TestMethod]
    public async Task RoleRepository_GetByNamesAndUserIdsInRoles()
    {
        var leiding = new IdentityRole("Leiding") { NormalizedName = "LEIDING" };
        var bar = new IdentityRole("Bar") { NormalizedName = "BAR" };
        _context.Roles.AddRange(leiding, bar);
        _context.UserRoles.Add(new IdentityUserRole<string> { UserId = TestUser.Id, RoleId = leiding.Id });
        await _context.SaveChangesAsync(TestContext.CancellationToken);
        var sut = new RoleRepository(_context);

        var roles = await sut.GetByNames(["Leiding", "Unknown"]);
        var userIds = await sut.GetUserIdsInRoles(["Leiding", "Bar"]);

        roles.Select(r => r.Name).Should().BeEquivalentTo(new[] { "Leiding" });
        userIds.Should().BeEquivalentTo(new[] { TestUser.Id });
    }

    [TestMethod]
    public async Task PushSubscriptionRepository_UpsertRefreshesKeysAndDeleteRemoves()
    {
        var sut = new PushSubscriptionRepository(_context);

        await sut.Upsert(new PushSubscription { UserId = TestUser.Id, Endpoint = "https://push", P256dh = "a", Auth = "b" });
        await sut.Upsert(new PushSubscription { UserId = TestUser.Id, Endpoint = "https://push", P256dh = "c", Auth = "d" });

        var stored = await _context.PushSubscription.SingleAsync(TestContext.CancellationToken);
        stored.P256dh.Should().Be("c");
        stored.Auth.Should().Be("d");

        await sut.Delete(TestUser.Id, "https://push");
        (await _context.PushSubscription.CountAsync(TestContext.CancellationToken)).Should().Be(0);
    }

    [TestMethod]
    public async Task BlobStorageItemRepository_GetById()
    {
        var item = new BlobStorageItem { FileName = "file.png", ContentType = "image/png", Uri = "https://blob/file.png" };
        _context.BlobStorageItem.Add(item);
        await _context.SaveChangesAsync(TestContext.CancellationToken);
        var sut = new BlobStorageItemRepository(_context);

        (await sut.GetById(item.Id))!.FileName.Should().Be("file.png");
        (await sut.GetById(Guid.NewGuid())).Should().BeNull();
    }

    [TestMethod]
    public async Task UserImageRepository_AddGetDelete()
    {
        var sut = new UserImageRepository(_context);
        var image = new UserImage { FileName = "avatar.png", ContentType = "image/png", Uri = "https://blob/avatar.png" };

        await sut.Add(image);
        (await sut.GetById(image.Id)).Should().NotBeNull();

        await sut.Delete(image);
        (await sut.GetById(image.Id)).Should().BeNull();
    }
}
