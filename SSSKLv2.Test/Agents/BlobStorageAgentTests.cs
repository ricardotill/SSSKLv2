using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure;
using SSSKLv2.Agents;
using System.Configuration;

namespace SSSKLv2.Test.Agents;

[TestClass]
public class BlobStorageAgentTests
{
    private BlobStorageAgent _sut = null!;
    private IConfiguration _mockConfiguration = null!;
    private BlobServiceClient _mockBlobServiceClient = null!;
    private ILogger<BlobStorageAgent> _mockLogger = null!;
    private BlobContainerClient _mockContainerClient = null!;
    private BlobClient _mockBlobClient = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockConfiguration = Substitute.For<IConfiguration>();
        _mockBlobServiceClient = Substitute.For<BlobServiceClient>();
        _mockLogger = Substitute.For<ILogger<BlobStorageAgent>>();
        _mockContainerClient = Substitute.For<BlobContainerClient>();
        _mockBlobClient = Substitute.For<BlobClient>();

        // Setup configuration
        var mockConfigSection = Substitute.For<IConfigurationSection>();
        mockConfigSection["ContainerName"].Returns("test-container");
        _mockConfiguration.GetSection("Storage").Returns(mockConfigSection);

        // Setup blob service client
        _mockBlobServiceClient.GetBlobContainerClient("test-container").Returns(_mockContainerClient);
        _mockContainerClient.GetBlobClient(Arg.Any<string>()).Returns(_mockBlobClient);
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WhenContainerNameIsConfigured_ShouldCreateInstance()
    {
        // Act & Assert
        var act = () => new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        act.Should().NotThrow();
    }

    [TestMethod]
    public void Constructor_WhenContainerNameIsNull_ShouldThrowConfigurationErrorsException()
    {
        // Arrange
        var mockConfigSection = Substitute.For<IConfigurationSection>();
        mockConfigSection["ContainerName"].Returns((string?)null);
        _mockConfiguration.GetSection("Storage").Returns(mockConfigSection);

        // Act & Assert
        var act = () => new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        act.Should().Throw<ConfigurationErrorsException>()
            .WithMessage("No Blob Container name found in config");
    }

    #endregion

    #region UploadFileToBlobAsync Tests

    [TestMethod]
    public async Task UploadFileToBlobAsync_WhenContainerExists_ShouldUploadFileSuccessfully()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "test-file.jpg";
        var contentType = "image/jpeg";
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var expectedUri = new Uri("https://test.blob.core.windows.net/test-container/test-file.jpg");

        _mockContainerClient.CreateIfNotExistsAsync().Returns((Response<BlobContainerInfo>?)null);
        _mockBlobClient.Uri.Returns(expectedUri);

        // Act
        var result = await _sut.UploadFileToBlobAsync(fileName, contentType, fileStream);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be(fileName);
        result.ContentType.Should().Be(contentType);
        result.Uri.Should().Be(expectedUri.ToString());

        await _mockBlobClient.Received(1).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        await _mockBlobClient.Received(1).UploadAsync(fileStream, Arg.Is<BlobHttpHeaders>(h => h.ContentType == contentType));
    }

    [TestMethod]
    public async Task UploadFileToBlobAsync_WhenContainerNeedsToBeCreated_ShouldCreateContainerAndUploadFile()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "test-file.pdf";
        var contentType = "application/pdf";
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var expectedUri = new Uri("https://test.blob.core.windows.net/test-container/test-file.pdf");

        var mockResponse = Substitute.For<Response<BlobContainerInfo>>();
        var mockRawResponse = Substitute.For<Response>();
        mockRawResponse.Status.Returns(201);
        mockResponse.GetRawResponse().Returns(mockRawResponse);

        _mockContainerClient.CreateIfNotExistsAsync().Returns(mockResponse);
        _mockBlobClient.Uri.Returns(expectedUri);

        // Act
        var result = await _sut.UploadFileToBlobAsync(fileName, contentType, fileStream);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be(fileName);
        result.ContentType.Should().Be(contentType);
        result.Uri.Should().Be(expectedUri.ToString());

        await _mockContainerClient.Received(1).SetAccessPolicyAsync(PublicAccessType.Blob);
    }

    [TestMethod]
    public async Task UploadFileToBlobAsync_WhenFileStreamIsEmpty_ShouldUploadEmptyFile()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "empty-file.txt";
        var contentType = "text/plain";
        var fileStream = new MemoryStream();
        var expectedUri = new Uri("https://test.blob.core.windows.net/test-container/empty-file.txt");

        _mockContainerClient.CreateIfNotExistsAsync().Returns((Response<BlobContainerInfo>?)null);
        _mockBlobClient.Uri.Returns(expectedUri);

        // Act
        var result = await _sut.UploadFileToBlobAsync(fileName, contentType, fileStream);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be(fileName);
        result.ContentType.Should().Be(contentType);
        result.Uri.Should().Be(expectedUri.ToString());
    }

    [TestMethod]
    public async Task UploadFileToBlobAsync_WhenBlobClientThrowsException_ShouldPropagateException()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "test-file.jpg";
        var contentType = "image/jpeg";
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });

        _mockContainerClient.CreateIfNotExistsAsync().Returns((Response<BlobContainerInfo>?)null);
        _mockBlobClient.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobHttpHeaders>())
            .ThrowsAsync(new RequestFailedException("Upload failed"));

        // Act & Assert
        var act = () => _sut.UploadFileToBlobAsync(fileName, contentType, fileStream);
        await act.Should().ThrowAsync<RequestFailedException>().WithMessage("Upload failed");
    }

    [TestMethod]
    public async Task UploadFileToBlobAsync_WithSpecialCharactersInFileName_ShouldHandleCorrectly()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "test file with spaces & special chars.jpg";
        var contentType = "image/jpeg";
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var expectedUri = new Uri("https://test.blob.core.windows.net/test-container/test file with spaces & special chars.jpg");

        _mockContainerClient.CreateIfNotExistsAsync().Returns((Response<BlobContainerInfo>?)null);
        _mockBlobClient.Uri.Returns(expectedUri);

        // Act
        var result = await _sut.UploadFileToBlobAsync(fileName, contentType, fileStream);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be(fileName);
        result.ContentType.Should().Be(contentType);
        result.Uri.Should().Be(expectedUri.ToString());
    }

    #endregion

    #region DeleteFileToBlobAsync Tests

    [TestMethod]
    public async Task DeleteFileToBlobAsync_WhenFileExists_ShouldDeleteSuccessfully()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "file-to-delete.jpg";

        _mockContainerClient.CreateIfNotExistsAsync().Returns((Response<BlobContainerInfo>?)null);

        // Act
        var result = await _sut.DeleteFileToBlobAsync(fileName);

        // Assert
        result.Should().BeTrue();
        await _mockBlobClient.Received(1).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
    }

    [TestMethod]
    public async Task DeleteFileToBlobAsync_WhenContainerNeedsToBeCreated_ShouldCreateContainerAndDeleteFile()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "file-to-delete.jpg";

        var mockResponse = Substitute.For<Response<BlobContainerInfo>>();
        var mockRawResponse = Substitute.For<Response>();
        mockRawResponse.Status.Returns(201);
        mockResponse.GetRawResponse().Returns(mockRawResponse);

        _mockContainerClient.CreateIfNotExistsAsync().Returns(mockResponse);

        // Act
        var result = await _sut.DeleteFileToBlobAsync(fileName);

        // Assert
        result.Should().BeTrue();
        await _mockContainerClient.Received(1).SetAccessPolicyAsync(PublicAccessType.Blob);
        await _mockBlobClient.Received(1).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
    }

    [TestMethod]
    public async Task DeleteFileToBlobAsync_WhenFileDoesNotExist_ShouldReturnTrueAnyway()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "non-existent-file.jpg";

        _mockContainerClient.CreateIfNotExistsAsync().Returns((Response<BlobContainerInfo>?)null);

        // Act
        var result = await _sut.DeleteFileToBlobAsync(fileName);

        // Assert
        result.Should().BeTrue();
        await _mockBlobClient.Received(1).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
    }

    [TestMethod]
    public async Task DeleteFileToBlobAsync_WhenBlobClientThrowsException_ShouldPropagateException()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "file-to-delete.jpg";

        _mockContainerClient.CreateIfNotExistsAsync().Returns((Response<BlobContainerInfo>?)null);
        _mockBlobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots)
            .ThrowsAsync(new RequestFailedException("Delete failed"));

        // Act & Assert
        var act = () => _sut.DeleteFileToBlobAsync(fileName);
        await act.Should().ThrowAsync<RequestFailedException>().WithMessage("Delete failed");
    }

    [TestMethod]
    public async Task DeleteFileToBlobAsync_WithSpecialCharactersInFileName_ShouldHandleCorrectly()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "file with spaces & special chars.jpg";

        _mockContainerClient.CreateIfNotExistsAsync().Returns((Response<BlobContainerInfo>?)null);

        // Act
        var result = await _sut.DeleteFileToBlobAsync(fileName);

        // Assert
        result.Should().BeTrue();
        _mockContainerClient.Received(1).GetBlobClient(fileName);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [TestMethod]
    public async Task UploadFileToBlobAsync_WhenStreamIsDisposed_ShouldThrowException()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "test-file.jpg";
        var contentType = "image/jpeg";
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        await fileStream.DisposeAsync();

        _mockContainerClient.CreateIfNotExistsAsync().Returns((Response<BlobContainerInfo>?)null);

        // Act & Assert - Since the actual implementation may throw different exceptions when stream is disposed,
        // we should test that an exception is thrown rather than expecting a specific type
        var act = () => _sut.UploadFileToBlobAsync(fileName, contentType, fileStream);
        await act.Should().ThrowAsync<Exception>();
    }

    [TestMethod]
    public async Task UploadFileToBlobAsync_WhenFileNameIsEmpty_ShouldStillCreateBlobClient()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "";
        var contentType = "text/plain";
        var fileStream = new MemoryStream(new byte[] { 1 });
        var expectedUri = new Uri("https://test.blob.core.windows.net/test-container/");

        _mockContainerClient.CreateIfNotExistsAsync().Returns((Response<BlobContainerInfo>?)null);
        _mockBlobClient.Uri.Returns(expectedUri);

        // Act
        var result = await _sut.UploadFileToBlobAsync(fileName, contentType, fileStream);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be(fileName);
        _mockContainerClient.Received(1).GetBlobClient(fileName);
    }

    [TestMethod]
    public async Task DeleteFileToBlobAsync_WhenFileNameIsEmpty_ShouldStillCreateBlobClient()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "";

        _mockContainerClient.CreateIfNotExistsAsync().Returns((Response<BlobContainerInfo>?)null);

        // Act
        var result = await _sut.DeleteFileToBlobAsync(fileName);

        // Assert
        result.Should().BeTrue();
        _mockContainerClient.Received(1).GetBlobClient(fileName);
    }

    [TestMethod]
    public async Task UploadFileToBlobAsync_WhenContainerCreateThrowsException_ShouldPropagateException()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "test-file.jpg";
        var contentType = "image/jpeg";
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });

        _mockContainerClient.CreateIfNotExistsAsync()
            .ThrowsAsync(new RequestFailedException("Container creation failed"));

        // Act & Assert
        var act = () => _sut.UploadFileToBlobAsync(fileName, contentType, fileStream);
        await act.Should().ThrowAsync<RequestFailedException>().WithMessage("Container creation failed");
    }

    [TestMethod]
    public async Task DeleteFileToBlobAsync_WhenContainerCreateThrowsException_ShouldPropagateException()
    {
        // Arrange
        _sut = new BlobStorageAgent(_mockConfiguration, _mockBlobServiceClient, _mockLogger);
        var fileName = "file-to-delete.jpg";

        _mockContainerClient.CreateIfNotExistsAsync()
            .ThrowsAsync(new RequestFailedException("Container creation failed"));

        // Act & Assert
        var act = () => _sut.DeleteFileToBlobAsync(fileName);
        await act.Should().ThrowAsync<RequestFailedException>().WithMessage("Container creation failed");
    }

    #endregion
}
