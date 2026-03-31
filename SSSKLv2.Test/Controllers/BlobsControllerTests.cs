using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SSSKLv2.Agents;
using SSSKLv2.Controllers.v1;
using SSSKLv2.Data;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SSSKLv2.Test.Controllers;

[TestClass]
public class BlobsControllerTests
{
    private BlobsController _sut = null!;
    private IBlobStorageAgent _mockBlobAgent = null!;
    private ApplicationDbContext _context = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(dbContextOptions);
        _mockBlobAgent = Substitute.For<IBlobStorageAgent>();
        
        _sut = new BlobsController(_mockBlobAgent, _context);
    }

    [TestMethod]
    public async Task GetEventImage_WhenFound_ReturnsFile()
    {
        // Arrange
        var id = Guid.NewGuid();
        var blobItem = new BlobStorageItem 
        { 
            Id = id, 
            FileName = "event.png", 
            ContentType = "image/png" 
        };
        _context.BlobStorageItem.Add(blobItem);
        await _context.SaveChangesAsync();

        var stream = new MemoryStream(new byte[] { 0x01, 0x02 });
        _mockBlobAgent.OpenDownloadStreamAsync("event.png").Returns(stream);

        // Act
        var result = await _sut.GetEventImage(id);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        var fileResult = (FileStreamResult)result;
        fileResult.ContentType.Should().Be("image/png");
        await _mockBlobAgent.Received(1).OpenDownloadStreamAsync("event.png");
    }

    [TestMethod]
    public async Task GetEventImage_WhenNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _sut.GetEventImage(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task GetAchievementImage_WhenFound_ReturnsFile()
    {
        // Arrange
        var id = Guid.NewGuid();
        var blobItem = new BlobStorageItem 
        { 
            Id = id, 
            FileName = "ach.png", 
            ContentType = "image/png" 
        };
        _context.BlobStorageItem.Add(blobItem);
        await _context.SaveChangesAsync();

        var stream = new MemoryStream(new byte[] { 0x01, 0x02 });
        _mockBlobAgent.OpenDownloadStreamAsync("ach.png").Returns(stream);

        // Act
        var result = await _sut.GetAchievementImage(id);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        await _mockBlobAgent.Received(1).OpenDownloadStreamAsync("ach.png");
    }

    [TestMethod]
    public async Task GetProfilePicture_WhenFound_ReturnsFile()
    {
        // Arrange
        var id = Guid.NewGuid();
        var blobItem = new BlobStorageItem 
        { 
            Id = id, 
            FileName = "profile.png", 
            ContentType = "image/png" 
        };
        _context.BlobStorageItem.Add(blobItem);
        await _context.SaveChangesAsync();

        var stream = new MemoryStream(new byte[] { 0x01, 0x02 });
        _mockBlobAgent.OpenDownloadStreamAsync("profile.png").Returns(stream);

        // Act
        var result = await _sut.GetProfilePicture(id);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        await _mockBlobAgent.Received(1).OpenDownloadStreamAsync("profile.png");
    }

    [TestMethod]
    public async Task GetSocialPreview_WhenFound_ReturnsProcessedFile()
    {
        // Arrange
        var id = Guid.NewGuid();
        var blobItem = new BlobStorageItem 
        { 
            Id = id, 
            FileName = "social.png", 
            ContentType = "image/png" 
        };
        _context.BlobStorageItem.Add(blobItem);
        await _context.SaveChangesAsync();

        // 1x1 transparent PNG
        var pngBytes = new byte[] { 
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0x60, 0x00, 0x00, 0x00, 
            0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC, 0x33, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 
            0x42, 0x60, 0x82 
        };
        var stream = new MemoryStream(pngBytes);
        _mockBlobAgent.OpenDownloadStreamAsync("social.png").Returns(stream);

        // Act
        var result = await _sut.GetSocialPreview(id);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        var fileResult = (FileStreamResult)result;
        fileResult.ContentType.Should().Be("image/jpeg");
        await _mockBlobAgent.Received(1).OpenDownloadStreamAsync("social.png");
    }
}
