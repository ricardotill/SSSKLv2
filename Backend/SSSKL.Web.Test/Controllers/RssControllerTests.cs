using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SSSKL.Application.Features.Announcements.Queries.GetAllAnnouncements;
using SSSKL.Application.Features.Products.Queries.GetAvailableProducts;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;
using SSSKL.Web.Controllers.v1;

namespace SSSKL.Web.Test.Controllers;

[TestClass]
public class RssControllerTests
{
    private RssController _sut = null!;
    private ISender _sender = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _sender = Substitute.For<ISender>();
        _sut = new RssController(_sender);
        
        // Mock HttpContext for Request.Scheme, Request.Host, etc.
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("test.host");
        httpContext.Request.PathBase = new PathString("/base");
        
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [TestMethod]
    public async Task GetProductsFeed_ReturnsRssFile()
    {
        // Arrange
        IReadOnlyList<ProductDto> products =
        [
            new ProductDto { Name = "P1", Price = 1.5m, Stock = 10 },
            new ProductDto { Name = "P2", Price = 2.0m, Stock = 5 }
        ];
        _sender.Send(Arg.Any<GetAvailableProductsQuery>(), Arg.Any<CancellationToken>()).Returns(products);

        // Act
        var result = await _sut.GetProductsFeed();

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = (FileContentResult)result;
        fileResult.ContentType.Should().Be("application/rss+xml; charset=utf-8");
        fileResult.FileContents.Should().NotBeEmpty();
        
        await _sender.Received(1).Send(Arg.Any<GetAvailableProductsQuery>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetAnnouncementsFeed_ReturnsRssFile()
    {
        // Arrange
        var announcements = new List<Announcement>
        {
            new Announcement { Message = "M1", Description = "D1", Url = "https://link.com" },
            new Announcement { Message = "M2", Description = "D2" }
        };
        _sender.Send(Arg.Any<GetAllAnnouncementsQuery>(), Arg.Any<CancellationToken>()).Returns(announcements);

        // Act
        var result = await _sut.GetAnnouncementsFeed();

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = (FileContentResult)result;
        fileResult.ContentType.Should().Be("application/rss+xml; charset=utf-8");
        fileResult.FileContents.Should().NotBeEmpty();
        
        await _sender.Received(1).Send(Arg.Any<GetAllAnnouncementsQuery>(), Arg.Any<CancellationToken>());
    }
}
