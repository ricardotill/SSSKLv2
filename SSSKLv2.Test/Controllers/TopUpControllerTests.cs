using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SSSKLv2.Controllers.v1;
using SSSKLv2.Data;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Test.Controllers;

[TestClass]
public class TopUpControllerTests
{
    private ITopUpService _mockService = null!;
    private ILogger<TopUpController> _mockLogger = null!;
    private TopUpController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _mockService = Substitute.For<ITopUpService>();
        _mockLogger = Substitute.For<ILogger<TopUpController>>();
        _sut = new TopUpController(_mockService, _mockLogger);
    }

    [TestMethod]
    public async Task GetAll_ReturnsOkWithItems()
    {
        // Arrange
        var items = new List<TopUp>
        {
            new TopUp { Id = Guid.NewGuid(), User = new ApplicationUser { UserName = "u1" }, Saldo = 1.50m },
            new TopUp { Id = Guid.NewGuid(), User = new ApplicationUser { UserName = "u2" }, Saldo = 2.00m }
        };
        // Controller expects an authenticated user and calls GetAll(skip,take) and GetCount()
        _mockService.GetAll(Arg.Any<int>(), Arg.Any<int>()).Returns(Task.FromResult<IEnumerable<TopUp>>(items));
        _mockService.GetCount().Returns(items.Count);

        // Set authenticated user on controller (controller checks User.Identity.Name)
        _sut.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "admin") }, "TestAuth"))
            }
        };

        // Act
        var result = await _sut.GetAll();

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var expectedDtos = items.Select(t => new TopUpDto { Id = t.Id, UserName = t.User.UserName, Saldo = t.Saldo }).ToList();
        ok!.Value.Should().BeEquivalentTo(new PaginationObject<TopUpDto> { Items = expectedDtos, TotalCount = items.Count });
    }

    [TestMethod]
    public async Task GetPersonal_ReturnsOkWithItems()
    {
        // Arrange
        var username = "u1";
        var items = new List<TopUp>
        {
            new TopUp { Id = Guid.NewGuid(), User = new ApplicationUser { UserName = username }, Saldo = 5.00m }
        };
        _mockService.GetAll(Arg.Any<int>(), Arg.Any<int>()).Returns(Task.FromResult<IEnumerable<TopUp>>(items));
        _mockService.GetCount().Returns(items.Count);

        // Set authenticated user on controller
        _sut.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username) }, "TestAuth"))
            }
        };

        // Act
        var result = await _sut.GetPersonal(1, 1);

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var expectedDtos = items.Select(t => new TopUpDto { Id = t.Id, UserName = t.User.UserName, Saldo = t.Saldo }).ToList();
        ok!.Value.Should().BeEquivalentTo(new PaginationObject<TopUpDto> { Items = expectedDtos, TotalCount = items.Count });
    }

    [TestMethod]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var t = new TopUp { Id = id, User = new ApplicationUser { UserName = "u1" }, Saldo = 3.25m };
        _mockService.GetById(id.ToString()).Returns(Task.FromResult<TopUp?>(t));

        // Act
        var result = await _sut.GetById(id.ToString());

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var dto = ok!.Value as TopUpDto;
        dto.Should().NotBeNull();
        dto!.Should().BeEquivalentTo(new TopUpDto { Id = t.Id, UserName = t.User.UserName, Saldo = t.Saldo });
    }

    [TestMethod]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockService.GetById(Arg.Any<string>()).Returns(Task.FromException<TopUp?>(new Exception("not found")));

        // Act
        var result = await _sut.GetById(Guid.NewGuid().ToString());

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task Create_WithValidDto_ReturnsCreated_And_CallsService()
    {
        // Arrange
        var dto = new TopUpCreateDto { UserName = "u1", Saldo = 4.50m };

        // Act
        var result = await _sut.Create(dto);

        // Assert
        var created = result.Result as CreatedAtActionResult;
        created.Should().NotBeNull();
        var outDto = created!.Value as TopUpDto;
        outDto.Should().NotBeNull();
        await _mockService.Received(1).CreateTopUp(Arg.Any<TopUp>());
    }

    [TestMethod]
    public async Task Create_WithNull_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.Create(null);

        // Assert
        result.Result.Should().BeOfType<BadRequestResult>();
        await _mockService.DidNotReceive().CreateTopUp(Arg.Any<TopUp>());
    }

    [TestMethod]
    public async Task Create_WithInvalidDto_ReturnsBadRequest_And_DoesNotCallService()
    {
        // Arrange
        var dto = new TopUpCreateDto { UserName = "", Saldo = -1m };
        _sut.ModelState.AddModelError("UserName", "Required");

        // Act
        var result = await _sut.Create(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        await _mockService.DidNotReceive().CreateTopUp(Arg.Any<TopUp>());
    }

    [TestMethod]
    public async Task Delete_WhenFound_ReturnsNoContent_And_CallsService()
    {
        // Arrange
        var id = Guid.NewGuid();
        var t = new TopUp { Id = id, User = new ApplicationUser { UserName = "u1" }, Saldo = 1m };
        _mockService.GetById(id.ToString()).Returns(Task.FromResult<TopUp?>(t));

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _mockService.Received(1).DeleteTopUp(id);
    }

    [TestMethod]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockService.GetById(Arg.Any<string>()).Returns(Task.FromException<TopUp?>(new Exception("not found")));

        // Act
        var result = await _sut.Delete(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
