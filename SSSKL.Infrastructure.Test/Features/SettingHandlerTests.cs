using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SSSKL.Application.Features.Settings.Commands.UpdateGlobalSetting;
using SSSKL.Application.Features.Settings.Queries.GetGlobalSetting;
using SSSKL.Application.Features.Settings.Queries.GetTurnstileConfig;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;
using SSSKL.Infrastructure.Persistence;
using SSSKL.Infrastructure.Persistence.Repositories;

namespace SSSKL.Infrastructure.Test.Features;

[TestClass]
public class SettingHandlerTests
{
    private const string MaliciousHtml = @"<script>alert('xss')</script><div onload=""alert('xss')"""
        + @"style=""background-color: rgba(0, 0, 0, 1)"">Test<img src=""test.png"""
        + @"style=""background-image: url(javascript:alert('xss')); margin: 10px""></div>";
    private const string SanitizedHtml = @"<div style=""background-color: rgba(0, 0, 0, 1)"">"
        + @"Test<img src=""test.png"" style=""margin: 10px""></div>";

    private ApplicationDbContext _context = null!;
    private GlobalSettingRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _repository = new GlobalSettingRepository(_context);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private Task<Contracts.Dto.Api.v1.GlobalSettingDto> Get(string key, bool isAdmin = false) =>
        new GetGlobalSettingHandler(_repository).Handle(new GetGlobalSettingQuery(key, isAdmin), CancellationToken.None);

    private Task Update(string key, string value) =>
        new UpdateGlobalSettingHandler(_repository).Handle(new UpdateGlobalSettingCommand(key, value), CancellationToken.None);

    [TestMethod]
    public async Task GetSetting_WhenExists_ReturnsSetting()
    {
        _context.GlobalSetting.Add(new GlobalSetting { Key = "test-key", Value = "test-value" });
        await _context.SaveChangesAsync();

        var result = await Get("test-key");

        result.Key.Should().Be("test-key");
        result.Value.Should().Be("test-value");
    }

    [TestMethod]
    public async Task GetSetting_WhenNotExists_ThrowsNotFound()
    {
        var act = () => Get("non-existent");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task GetSetting_WriteOnlyKey_ThrowsUnauthorizedEvenForAdmins()
    {
        var act = () => Get(GlobalSettingsKeys.WriteOnlyKeys.First(), isAdmin: true);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task GetSetting_SensitiveKeyAsNonAdmin_ThrowsUnauthorized()
    {
        var sensitive = GlobalSettingsKeys.SensitiveKeys.First(k => !GlobalSettingsKeys.WriteOnlyKeys.Contains(k));

        var act = () => Get(sensitive, isAdmin: false);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task UpdateSetting_WhenNotExists_CreatesNewSetting()
    {
        await Update("new-key", "new-value");

        var setting = await _context.GlobalSetting.FirstOrDefaultAsync(s => s.Key == "new-key");
        setting!.Value.Should().Be("new-value");
    }

    [TestMethod]
    public async Task UpdateSetting_WhenExists_UpdatesValue()
    {
        _context.GlobalSetting.Add(new GlobalSetting { Key = "existing-key", Value = "old-value" });
        await _context.SaveChangesAsync();

        await Update("existing-key", "updated-value");

        var updated = await _context.GlobalSetting.FirstOrDefaultAsync(s => s.Key == "existing-key");
        updated!.Value.Should().Be("updated-value");
        updated.UpdatedOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task UpdateSetting_WithMaliciousHtml_ShouldSanitizeHtml()
    {
        await Update("test-key", MaliciousHtml);

        var settingInDb = await _context.GlobalSetting.FirstOrDefaultAsync(s => s.Key == "test-key");
        settingInDb!.Value.Should().Be(SanitizedHtml);
    }

    [TestMethod]
    public async Task UpdateSetting_ExistingSettingWithMaliciousHtml_ShouldSanitizeHtml()
    {
        _context.GlobalSetting.Add(new GlobalSetting { Key = "test-key", Value = "Old Value", UpdatedOn = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        await Update("test-key", MaliciousHtml);

        var settingInDb = await _context.GlobalSetting.FirstOrDefaultAsync(s => s.Key == "test-key");
        settingInDb!.Value.Should().Be(SanitizedHtml);
    }

    [TestMethod]
    public async Task UpdateSetting_SensitiveKey_StoresValueVerbatim()
    {
        var sensitive = GlobalSettingsKeys.SensitiveKeys.First();

        await Update(sensitive, "<b>secret</b>");

        var settingInDb = await _context.GlobalSetting.FirstOrDefaultAsync(s => s.Key == sensitive);
        settingInDb!.Value.Should().Be("<b>secret</b>");
    }

    [TestMethod]
    public async Task GetTurnstileConfig_MapsClientConfig()
    {
        var turnstile = Substitute.For<ITurnstileService>();
        turnstile.GetClientConfigAsync(Arg.Any<CancellationToken>()).Returns(new TurnstileClientConfig(true, "site-key"));

        var result = await new GetTurnstileConfigHandler(turnstile).Handle(new GetTurnstileConfigQuery(), CancellationToken.None);

        result.Enabled.Should().BeTrue();
        result.SiteKey.Should().Be("site-key");
    }
}
