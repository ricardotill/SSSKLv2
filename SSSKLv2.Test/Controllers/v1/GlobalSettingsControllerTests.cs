using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Controllers.v1;
using SSSKLv2.Data;
using SSSKLv2.Dto.Api.v1;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace SSSKLv2.Test.Controllers.v1;

[TestClass]
public class GlobalSettingsControllerTests
{
    private ApplicationDbContext _context = null!;
    private GlobalSettingsController _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        _sut = new GlobalSettingsController(_context);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    public async Task UpdateSetting_WithMaliciousHtml_ShouldSanitizeHtml()
    {
        // Arrange
        var html = @"<script>alert('xss')</script><div onload=""alert('xss')"""
            + @"style=""background-color: rgba(0, 0, 0, 1)"">Test<img src=""test.png"""
            + @"style=""background-image: url(javascript:alert('xss')); margin: 10px""></div>";
        var expected = @"<div style=""background-color: rgba(0, 0, 0, 1)"">"
            + @"Test<img src=""test.png"" style=""margin: 10px""></div>";

        var dto = new GlobalSettingUpdateDto { Value = html };

        // Act
        var result = await _sut.UpdateSetting("test-key", dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var settingInDb = await _context.GlobalSetting.FirstOrDefaultAsync(s => s.Key == "test-key");
        settingInDb.Should().NotBeNull();
        settingInDb!.Value.Should().Be(expected);
    }

    [TestMethod]
    public async Task UpdateSetting_ExistingSettingWithMaliciousHtml_ShouldSanitizeHtml()
    {
        // Arrange
        _context.GlobalSetting.Add(new GlobalSetting { Key = "test-key", Value = "Old Value", UpdatedOn = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        
        var html = @"<script>alert('xss')</script><div onload=""alert('xss')"""
            + @"style=""background-color: rgba(0, 0, 0, 1)"">Test<img src=""test.png"""
            + @"style=""background-image: url(javascript:alert('xss')); margin: 10px""></div>";
        var expected = @"<div style=""background-color: rgba(0, 0, 0, 1)"">"
            + @"Test<img src=""test.png"" style=""margin: 10px""></div>";

        var dto = new GlobalSettingUpdateDto { Value = html };

        // Act
        var result = await _sut.UpdateSetting("test-key", dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var settingInDb = await _context.GlobalSetting.FirstOrDefaultAsync(s => s.Key == "test-key");
        settingInDb.Should().NotBeNull();
        settingInDb!.Value.Should().Be(expected);
    }
}
