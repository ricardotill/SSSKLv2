using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKLv2.Controllers.v1;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Dto;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Test.Controllers;

[TestClass]
public class OrderControllerTests
{
    private IOrderService _orderService = null!;
    private ILogger<OrderController> _logger = null!;
    private IProductService _productService = null!;
    private IApplicationUserService _applicationUserService = null!;
    private OrderController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _orderService = Substitute.For<IOrderService>();
        _logger = Substitute.For<ILogger<OrderController>>();
        _productService = Substitute.For<IProductService>();
        _applicationUserService = Substitute.For<IApplicationUserService>();

        _sut = new OrderController(_orderService, _logger, _productService, _applicationUserService);
    }

    [TestMethod]
    public async Task Delete_AdminUser_DeletesAnyOrder()
    {
        // Arrange
        var id = Guid.NewGuid();
        SetUserContext(Guid.NewGuid().ToString(), "admin", isAdmin: true);

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _orderService.Received(1).DeleteOrder(id);
        await _orderService.DidNotReceive().GetOrderById(Arg.Any<Guid>());
    }

    [TestMethod]
    public async Task Delete_NonAdminOwner_DeletesOwnOrder()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var username = "user1";
        SetUserContext(userId, username, isAdmin: false);
        _orderService.GetOrderById(id).Returns(Task.FromResult(new Order
        {
            Id = id,
            User = new ApplicationUser { Id = userId, UserName = username },
            ProductNaam = "Test",
            Amount = 1,
            Paid = 1m
        }));

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _orderService.Received(1).GetOrderById(id);
        await _orderService.Received(1).DeleteOrder(id);
    }

    [TestMethod]
    public async Task Delete_NonAdminNonOwner_ReturnsForbid()
    {
        // Arrange
        var id = Guid.NewGuid();
        SetUserContext(Guid.NewGuid().ToString(), "user1", isAdmin: false);
        _orderService.GetOrderById(id).Returns(Task.FromResult(new Order
        {
            Id = id,
            User = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "user2" },
            ProductNaam = "Test",
            Amount = 1,
            Paid = 1m
        }));

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<ForbidResult>();
        await _orderService.Received(1).GetOrderById(id);
        await _orderService.DidNotReceive().DeleteOrder(Arg.Any<Guid>());
    }

    [TestMethod]
    public async Task Delete_NonAdminWithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        var id = Guid.NewGuid();
        SetUserContext(null, "user1", isAdmin: false);

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
        await _orderService.DidNotReceive().GetOrderById(Arg.Any<Guid>());
        await _orderService.DidNotReceive().DeleteOrder(Arg.Any<Guid>());
    }

    [TestMethod]
    public async Task Delete_NonAdminOrderNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        SetUserContext(Guid.NewGuid().ToString(), "user1", isAdmin: false);
        _orderService.GetOrderById(id).Returns(Task.FromException<Order>(new NotFoundException("Order not found")));

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        await _orderService.Received(1).GetOrderById(id);
        await _orderService.DidNotReceive().DeleteOrder(Arg.Any<Guid>());
    }

    private void SetUserContext(string? userId, string? username, bool isAdmin)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            claims.Add(new Claim("sub", userId));
        }

        if (!string.IsNullOrWhiteSpace(username))
        {
            claims.Add(new Claim(ClaimTypes.Name, username));
        }

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
    }

    [TestMethod]
    public async Task GetAll_ReturnsOkWithItems()
    {
        var list = new List<Order> { new Order { Id = Guid.NewGuid(), ProductNaam = "P1" } };
        _orderService.GetAll(0, 15).Returns(list);
        _orderService.GetCount().Returns(1);

        var result = await _sut.GetAll(0, 15);

        result.Should().BeOfType<OkObjectResult>().Which.Value.As<PaginationObject<OrderDto>>().TotalCount.Should().Be(1);
    }

    [TestMethod]
    public async Task GetPersonal_ReturnsOkWithItems()
    {
        var username = "user1";
        SetUserContext("u1", username, false);
        var list = new List<Order> { new Order { Id = Guid.NewGuid(), ProductNaam = "P1" } };
        _orderService.GetPersonal(username, 0, 15).Returns(list);
        _orderService.GetPersonalCount(username).Returns(1);

        var result = await _sut.GetPersonal(0, 15);

        result.Should().BeOfType<OkObjectResult>().Which.Value.As<PaginationObject<OrderDto>>().TotalCount.Should().Be(1);
    }

    [TestMethod]
    public async Task GetPersonal_WhenUsernameMissing_ReturnsUnauthorized()
    {
        // Arrange
        SetUserContext("u1", null, false);

        // Act
        var result = await _sut.GetPersonal(0, 15);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [TestMethod]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _orderService.GetOrderById(id).Returns(new Order { Id = id, ProductNaam = "P1" });

        var result = await _sut.GetById(id);

        result.Should().BeOfType<OkObjectResult>().Which.Value.As<OrderDto>().Id.Should().Be(id);
    }

    [TestMethod]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _orderService.GetOrderById(id).Throws(new NotFoundException(""));

        // Act
        var result = await _sut.GetById(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task GetLatest_ReturnsOk()
    {
        var list = new List<Order> { new Order { Id = Guid.NewGuid() } };
        _orderService.GetLatestOrders(5).Returns(list);

        var result = await _sut.GetLatest(5);

        result.Should().BeOfType<OkObjectResult>().Which.Value.As<IEnumerable<OrderDto>>().Should().HaveCount(1);
    }

    [TestMethod]
    public async Task GetOrderInitialize_ReturnsOk()
    {
        _productService.GetAllAvailable().Returns(new List<Product> { new Product { Id = Guid.NewGuid(), Name = "P1" } });
        _applicationUserService.GetAllUsers().Returns(new List<ApplicationUser> { new ApplicationUser { Id = "u1", UserName = "un1" } });

        var result = await _sut.GetOrderInitialize();

        result.Should().BeOfType<OkObjectResult>().Which.Value.As<OrderInitializeDto>().Products.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task Create_WithValidDto_ReturnsOk()
    {
        var dto = new OrderSubmitDto { Products = new List<Guid>(), Users = new List<Guid>() };
        
        var result = await _sut.Create(dto);

        result.Should().BeOfType<OkResult>();
        await _orderService.Received(1).CreateOrder(dto);
    }

    [TestMethod]
    public async Task Create_WhenNullDto_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.Create(null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }

    [TestMethod]
    public async Task Create_WhenServiceThrowsNotFound_ReturnsNotFound()
    {
        // Arrange
        var dto = new OrderSubmitDto();
        _orderService.CreateOrder(dto).Throws(new NotFoundException(""));

        // Act
        var result = await _sut.Create(dto);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task Create_WhenServiceThrowsGenericException_ReturnsProblem()
    {
        // Arrange
        var dto = new OrderSubmitDto();
        _orderService.CreateOrder(dto).Throws(new Exception("Fail"));

        // Act
        var result = await _sut.Create(dto);

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    [TestMethod]
    public async Task ExportCsv_ReturnsFile()
    {
        _orderService.ExportOrdersFromPastTwoYearsToCsvAsync().Returns("csv,content");

        var result = await _sut.ExportCsv();

        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("text/csv");
        fileResult.FileDownloadName.Should().Be("orders_last_2_years.csv");
    }

    [TestMethod]
    public async Task Delete_AdminOrderNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        SetUserContext("adminId", "admin", true);
        _orderService.DeleteOrder(id).Returns(Task.FromException(new NotFoundException("")));

        var result = await _sut.Delete(id);

        result.Should().BeOfType<NotFoundResult>();
    }
}


