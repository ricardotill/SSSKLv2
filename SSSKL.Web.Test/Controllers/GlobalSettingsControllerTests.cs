using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;
using SSSKL.Infrastructure.Persistence;
using SSSKL.Infrastructure.Persistence.Repositories;
using SSSKL.Web.Controllers.v1;
using SSSKL.Web.Test.Controllers.v1;

namespace SSSKL.Web.Test.Controllers;

[TestClass]
public class GlobalSettingsControllerTests
{
    private GlobalSettingsController _sut = null!;
    private ApplicationDbContext _context = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new GlobalSettingsController(new GlobalSettingRepository(_context));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    public async Task GetSetting_WhenExists_ReturnsOk()
    {
        // Arrange
        var setting = new GlobalSetting { Key = "test-key", Value = "test-value" };
        _context.GlobalSetting.Add(setting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetSetting("test-key");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var value = (result.Result as OkObjectResult)?.Value as GlobalSettingDto;
        value.Should().NotBeNull();
        value!.Key.Should().Be("test-key");
        value.Value.Should().Be("test-value");
    }

    [TestMethod]
    public async Task GetSetting_WhenNotExists_ReturnsNotFound()
    {
        // Act
        var result = await _sut.GetSetting("non-existent");

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task UpdateSetting_WhenNotExists_CreatesNewSetting()
    {
        // Arrange
        var dto = new GlobalSettingUpdateDto { Value = "new-value" };

        // Act
        var result = await _sut.UpdateSetting("new-key", dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var setting = await _context.GlobalSetting.FirstOrDefaultAsync(s => s.Key == "new-key");
        setting.Should().NotBeNull();
        setting!.Value.Should().Be("new-value");
    }

    [TestMethod]
    public async Task UpdateSetting_WhenExists_UpdatesValue()
    {
        // Arrange
        var setting = new GlobalSetting { Key = "existing-key", Value = "old-value" };
        _context.GlobalSetting.Add(setting);
        await _context.SaveChangesAsync();
        
        var dto = new GlobalSettingUpdateDto { Value = "updated-value" };

        // Act
        var result = await _sut.UpdateSetting("existing-key", dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var updated = await _context.GlobalSetting.FirstOrDefaultAsync(s => s.Key == "existing-key");
        updated.Should().NotBeNull();
        updated!.Value.Should().Be("updated-value");
        updated.UpdatedOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
