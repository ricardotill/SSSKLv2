using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SixLabors.ImageSharp;
using SSSKL.Application.Features.Blobs.Queries.GetBlobImage;
using SSSKL.Application.Features.Blobs.Queries.GetSocialPreview;
using SSSKL.Contracts.Agents;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;
using SSSKL.Infrastructure.Persistence;
using SSSKL.Infrastructure.Persistence.Repositories;

namespace SSSKL.Infrastructure.Test.Features;

[TestClass]
public class BlobHandlerTests
{
    // 1x1 transparent PNG
    private static readonly byte[] PngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0x60, 0x00, 0x00, 0x00,
        0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC, 0x33, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82
    ];

    private ApplicationDbContext _context = null!;
    private IBlobStorageAgent _blobAgent = null!;
    private BlobStorageItemRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _blobAgent = Substitute.For<IBlobStorageAgent>();
        _repository = new BlobStorageItemRepository(_context);
    }

    [TestCleanup]
    public void TestCleanup() => _context.Dispose();

    private async Task<Guid> AddBlob(string fileName, string contentType)
    {
        var item = new BlobStorageItem { Id = Guid.NewGuid(), FileName = fileName, ContentType = contentType };
        _context.BlobStorageItem.Add(item);
        await _context.SaveChangesAsync();
        return item.Id;
    }

    [TestMethod]
    public async Task GetBlobImage_WhenFound_ReturnsStreamWithContentType()
    {
        var id = await AddBlob("event.png", "image/png");
        var stream = new MemoryStream([0x01, 0x02]);
        _blobAgent.OpenDownloadStreamAsync("event.png").Returns(stream);

        var result = await new GetBlobImageHandler(_repository, _blobAgent).Handle(new GetBlobImageQuery(id), CancellationToken.None);

        result.ContentType.Should().Be("image/png");
        result.Content.Should().BeSameAs(stream);
    }

    [TestMethod]
    public async Task GetBlobImage_WhenNotFound_ThrowsNotFound()
    {
        var act = () => new GetBlobImageHandler(_repository, _blobAgent).Handle(new GetBlobImageQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task GetBlobImage_WithUnsupportedContentType_ThrowsArgumentException()
    {
        var id = await AddBlob("doc.pdf", "application/pdf");

        var act = () => new GetBlobImageHandler(_repository, _blobAgent).Handle(new GetBlobImageQuery(id), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        await _blobAgent.DidNotReceive().OpenDownloadStreamAsync(Arg.Any<string>());
    }

    [TestMethod]
    public async Task GetSocialPreview_WhenFound_ReturnsJpegAtLeast300Wide()
    {
        var id = await AddBlob("social.png", "image/png");
        _blobAgent.OpenDownloadStreamAsync("social.png").Returns(new MemoryStream(PngBytes));

        var result = await new GetSocialPreviewHandler(_repository, _blobAgent).Handle(new GetSocialPreviewQuery(id), CancellationToken.None);

        using var preview = await Image.LoadAsync(result.Content);
        preview.Metadata.DecodedImageFormat!.DefaultMimeType.Should().Be("image/jpeg");
        preview.Width.Should().Be(300);
    }
}
