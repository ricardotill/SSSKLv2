using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SSSKL.Application.Features.Blobs.Queries.GetBlobImage;
using SSSKL.Application.Features.Blobs.Queries.GetSocialPreview;
using SSSKL.Web.Controllers.v1;

namespace SSSKL.Web.Test.Controllers;

[TestClass]
public class BlobsControllerTests
{
    private ISender _sender = null!;
    private BlobsController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _sender = Substitute.For<ISender>();
        _sut = new BlobsController(_sender)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [TestMethod]
    public async Task GetEventImage_ReturnsFileWithContentType()
    {
        var id = Guid.NewGuid();
        _sender.Send(new GetBlobImageQuery(id), Arg.Any<CancellationToken>()).Returns(new BlobImage(new MemoryStream(), "image/png"));

        var result = await _sut.GetEventImage(id);

        result.Should().BeOfType<FileStreamResult>().Which.ContentType.Should().Be("image/png");
    }

    [TestMethod]
    public async Task GetSocialPreview_ReturnsJpegWithCacheHeaders()
    {
        var id = Guid.NewGuid();
        var createdOn = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        _sender.Send(new GetSocialPreviewQuery(id), Arg.Any<CancellationToken>()).Returns(new SocialPreview(new MemoryStream(), createdOn));

        var result = await _sut.GetSocialPreview(id);

        result.Should().BeOfType<FileStreamResult>().Which.ContentType.Should().Be("image/jpeg");
        _sut.Response.Headers.ETag.ToString().Should().Be($"\"{id:N}\"");
        _sut.Response.Headers.LastModified.ToString().Should().Be(createdOn.ToString("R"));
    }
}
