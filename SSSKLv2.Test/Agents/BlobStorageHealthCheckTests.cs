using Azure;
using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKLv2.Agents;

namespace SSSKLv2.Test.Agents;

[TestClass]
public class BlobStorageHealthCheckTests
{
    private BlobServiceClient _storage = null!;
    private BlobContainerClient _container = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _storage = Substitute.For<BlobServiceClient>();
        _container = Substitute.For<BlobContainerClient>();
        _storage.GetBlobContainerClient("images").Returns(_container);
    }

    private static IConfiguration Config(string? containerName) => new ConfigurationBuilder()
        .AddInMemoryCollection([new KeyValuePair<string, string?>("Storage:ContainerName", containerName)])
        .Build();

    private Task<HealthCheckResult> Check(string? containerName) =>
        new BlobStorageHealthCheck(_storage, Config(containerName)).CheckHealthAsync(new HealthCheckContext());

    [TestMethod]
    public async Task CheckHealthAsync_ContainerExists_IsHealthy()
    {
        _container.ExistsAsync(Arg.Any<CancellationToken>()).Returns(Response.FromValue(true, Substitute.For<Response>()));

        (await Check("images")).Status.Should().Be(HealthStatus.Healthy);
    }

    [TestMethod]
    public async Task CheckHealthAsync_ContainerMissing_IsUnhealthy()
    {
        _container.ExistsAsync(Arg.Any<CancellationToken>()).Returns(Response.FromValue(false, Substitute.For<Response>()));

        (await Check("images")).Status.Should().Be(HealthStatus.Unhealthy);
    }

    [TestMethod]
    public async Task CheckHealthAsync_StorageThrows_IsUnhealthy()
    {
        _container.ExistsAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new RequestFailedException(403, "AuthorizationPermissionMismatch"));

        var result = await Check("images");

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<RequestFailedException>();
    }

    [TestMethod]
    public async Task CheckHealthAsync_NoContainerConfigured_IsUnhealthy()
    {
        (await Check(null)).Status.Should().Be(HealthStatus.Unhealthy);
    }
}
