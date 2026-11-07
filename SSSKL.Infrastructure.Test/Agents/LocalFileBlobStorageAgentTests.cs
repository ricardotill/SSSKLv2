using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using SSSKL.Infrastructure.Agents;

namespace SSSKL.Infrastructure.Test.Agents;

[TestClass]
public class LocalFileBlobStorageAgentTests
{
    public TestContext TestContext { get; set; } = null!;

    private string _contentRoot = null!;
    private LocalFileBlobStorageAgent _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), $"ssskl-blobs-{Guid.NewGuid():N}");
        _sut = CreateAgent(blobRoot: "blobs");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_contentRoot)) Directory.Delete(_contentRoot, recursive: true);
    }

    private LocalFileBlobStorageAgent CreateAgent(string? blobRoot)
    {
        var settings = new Dictionary<string, string?>();
        if (blobRoot != null) settings["E2E:BlobRoot"] = blobRoot;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(_contentRoot);
        return new LocalFileBlobStorageAgent(configuration, environment);
    }

    private static MemoryStream Content(string text) => new(Encoding.UTF8.GetBytes(text));

    [TestMethod]
    public void Constructor_CreatesConfiguredRootUnderContentRoot()
    {
        Directory.Exists(Path.Combine(_contentRoot, "blobs")).Should().BeTrue();
    }

    [TestMethod]
    public void Constructor_WithoutConfiguration_UsesDefaultRoot()
    {
        CreateAgent(blobRoot: null);

        Directory.Exists(Path.Combine(_contentRoot, "e2e-blobs")).Should().BeTrue();
    }

    [TestMethod]
    public async Task UploadFileToBlobAsync_WritesFileAndReturnsItem()
    {
        var item = await _sut.UploadFileToBlobAsync("photo.png", " image/png ", Content("hello"));

        var path = Path.Combine(_contentRoot, "blobs", "photo.png");
        (await File.ReadAllTextAsync(path, TestContext.CancellationToken)).Should().Be("hello");
        item.FileName.Should().Be("photo.png");
        item.ContentType.Should().Be("image/png");
        item.Uri.Should().Be(new Uri(path).AbsoluteUri);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public async Task UploadFileToBlobAsync_WithoutContentType_UsesOctetStream(string? contentType)
    {
        var item = await _sut.UploadFileToBlobAsync("file.bin", contentType!, Content("x"));

        item.ContentType.Should().Be("application/octet-stream");
    }

    [TestMethod]
    public async Task UploadFileToBlobAsync_OverwritesExistingFile()
    {
        await _sut.UploadFileToBlobAsync("file.txt", "text/plain", Content("first version"));
        await _sut.UploadFileToBlobAsync("file.txt", "text/plain", Content("second"));

        (await File.ReadAllTextAsync(Path.Combine(_contentRoot, "blobs", "file.txt"), TestContext.CancellationToken)).Should().Be("second");
    }

    [TestMethod]
    public async Task OpenDownloadStreamAsync_ReturnsUploadedContent()
    {
        await _sut.UploadFileToBlobAsync("file.txt", "text/plain", Content("payload"));

        await using var stream = await _sut.OpenDownloadStreamAsync("file.txt");
        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync(TestContext.CancellationToken)).Should().Be("payload");
    }

    [TestMethod]
    public async Task OpenDownloadStreamAsync_MissingFile_Throws()
    {
        var act = () => _sut.OpenDownloadStreamAsync("missing.txt");

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [TestMethod]
    public async Task DeleteFileToBlobAsync_RemovesFile()
    {
        await _sut.UploadFileToBlobAsync("file.txt", "text/plain", Content("x"));

        var result = await _sut.DeleteFileToBlobAsync("file.txt");

        result.Should().BeTrue();
        File.Exists(Path.Combine(_contentRoot, "blobs", "file.txt")).Should().BeFalse();
    }

    [TestMethod]
    public async Task DeleteFileToBlobAsync_MissingFile_ReturnsTrue()
    {
        (await _sut.DeleteFileToBlobAsync("missing.txt")).Should().BeTrue();
    }

    [TestMethod]
    [DataRow("../outside.txt")]
    [DataRow("../../outside.txt")]
    [DataRow("sub/../../outside.txt")]
    public async Task PathTraversal_IsRejected(string fileName)
    {
        await FluentActions.Awaiting(() => _sut.UploadFileToBlobAsync(fileName, "text/plain", Content("x")))
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => _sut.OpenDownloadStreamAsync(fileName))
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => _sut.DeleteFileToBlobAsync(fileName))
            .Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task RootFolderItself_IsRejected()
    {
        // "." resolves to the root folder, which is not a file inside it.
        await FluentActions.Awaiting(() => _sut.DeleteFileToBlobAsync("."))
            .Should().ThrowAsync<ArgumentException>();
    }
}
