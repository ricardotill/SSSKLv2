using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Application.Features.Orders.Commands.CreateOrder;
using SSSKL.Application.Features.Orders.Commands.DeleteOrder;
using SSSKL.Application.Features.Orders.Export;
using SSSKL.Application.Features.Orders.Queries.GetLatestOrders;
using SSSKL.Application.Features.Orders.Queries.GetOrderById;
using SSSKL.Application.Features.Orders.Queries.GetOrderInitialize;
using SSSKL.Application.Features.Orders.Queries.GetOrders;
using SSSKL.Application.Features.Orders.Queries.GetPersonalOrders;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Exceptions;
using SSSKL.Web.Controllers.v1;

namespace SSSKL.Web.Test.Controllers;

[TestClass]
public class OrderControllerTests
{
    private ISender _sender = null!;
    private OrderController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _sender = Substitute.For<ISender>();
        _sut = new OrderController(_sender);
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
    public async Task Delete_AdminUser_SendsAdminDeleteCommand()
    {
        // Arrange
        var id = Guid.NewGuid();
        var adminId = Guid.NewGuid().ToString();
        SetUserContext(adminId, "admin", isAdmin: true);

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new DeleteOrderCommand(id, adminId, true), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Delete_NonAdmin_SendsOwnerDeleteCommand()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        SetUserContext(userId, "user1", isAdmin: false);

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new DeleteOrderCommand(id, userId, false), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Delete_NonAdminNonOwner_PropagatesUnauthorizedAccess()
    {
        // Arrange
        SetUserContext(Guid.NewGuid().ToString(), "user1", isAdmin: false);
        _sender.Send(Arg.Any<DeleteOrderCommand>(), Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var act = () => _sut.Delete(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task Delete_NonAdminWithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetUserContext(null, "user1", isAdmin: false);

        // Act
        var result = await _sut.Delete(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
        await _sender.DidNotReceive().Send(Arg.Any<DeleteOrderCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Delete_OrderNotFound_PropagatesNotFoundException()
    {
        // Arrange
        SetUserContext("adminId", "admin", true);
        _sender.Send(Arg.Any<DeleteOrderCommand>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("Order not found"));

        // Act
        var act = () => _sut.Delete(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task GetAll_ReturnsOkWithItems()
    {
        var page = new PaginationObject<OrderDto> { Items = [new OrderDto { Id = Guid.NewGuid() }], TotalCount = 1 };
        _sender.Send(new GetOrdersQuery(0, 15), Arg.Any<CancellationToken>()).Returns(page);

        var result = await _sut.GetAll(0, 15);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
    }

    [TestMethod]
    public async Task GetPersonal_ReturnsOkWithItems()
    {
        SetUserContext("u1", "user1", false);
        var page = new PaginationObject<OrderDto> { Items = [new OrderDto { Id = Guid.NewGuid() }], TotalCount = 1 };
        _sender.Send(new GetPersonalOrdersQuery("user1", 0, 15), Arg.Any<CancellationToken>()).Returns(page);

        var result = await _sut.GetPersonal(0, 15);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
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
        SetUserContext("u1", "user1", isAdmin: false);
        _sender.Send(new GetOrderByIdQuery(id, "u1", false), Arg.Any<CancellationToken>()).Returns(new OrderDto { Id = id });

        var result = await _sut.GetById(id);

        result.Should().BeOfType<OkObjectResult>().Which.Value.As<OrderDto>().Id.Should().Be(id);
    }

    [TestMethod]
    public async Task GetById_AdminUser_SendsAdminQuery()
    {
        var id = Guid.NewGuid();
        SetUserContext("adminId", "admin", isAdmin: true);
        _sender.Send(new GetOrderByIdQuery(id, "adminId", true), Arg.Any<CancellationToken>()).Returns(new OrderDto { Id = id });

        var result = await _sut.GetById(id);

        result.Should().BeOfType<OkObjectResult>();
    }

    [TestMethod]
    public async Task GetById_NonAdminWithoutUserId_ReturnsUnauthorized()
    {
        SetUserContext(null, "user1", isAdmin: false);

        var result = await _sut.GetById(Guid.NewGuid());

        result.Should().BeOfType<UnauthorizedResult>();
        await _sender.DidNotReceive().Send(Arg.Any<GetOrderByIdQuery>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetById_WhenNotFound_PropagatesNotFoundException()
    {
        var id = Guid.NewGuid();
        SetUserContext("adminId", "admin", isAdmin: true);
        _sender.Send(new GetOrderByIdQuery(id, "adminId", true), Arg.Any<CancellationToken>()).Throws(new NotFoundException(""));

        var act = () => _sut.GetById(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task GetLatest_ReturnsOk()
    {
        IReadOnlyList<OrderDto> list = [new OrderDto { Id = Guid.NewGuid() }];
        _sender.Send(new GetLatestOrdersQuery(5), Arg.Any<CancellationToken>()).Returns(list);

        var result = await _sut.GetLatest(5);

        result.Should().BeOfType<OkObjectResult>().Which.Value.As<IEnumerable<OrderDto>>().Should().HaveCount(1);
    }

    [TestMethod]
    public async Task GetOrderInitialize_ReturnsOk()
    {
        var dto = new OrderInitializeDto { Products = [new OrderInitializeProductDto { Id = Guid.NewGuid(), Name = "P1" }] };
        _sender.Send(Arg.Any<GetOrderInitializeQuery>(), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.GetOrderInitialize();

        result.Should().BeOfType<OkObjectResult>().Which.Value.As<OrderInitializeDto>().Products.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task Create_WithValidDto_ReturnsOk()
    {
        var dto = new OrderSubmitDto { Products = new List<Guid>(), Users = new List<Guid>() };
        var userId = Guid.NewGuid().ToString();
        SetUserContext(userId, "user1", false);

        var result = await _sut.Create(dto);

        result.Should().BeOfType<OkResult>();
        await _sender.Received(1).Send(new CreateOrderCommand(dto, userId), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task StartCsvExport_WhenNoJobRunning_ReturnsAcceptedJob()
    {
        var userId = Guid.NewGuid().ToString();
        SetUserContext(userId, "admin", isAdmin: true);
        var job = new CsvExportJobDto
        {
            Id = Guid.NewGuid(),
            Status = CsvExportJobStatus.Pending,
            StartedByUserId = userId,
            FileName = "Orders_Export.csv"
        };
        _sender.Send(new StartOrderCsvExportCommand(userId), Arg.Any<CancellationToken>()).Returns(new StartOrderCsvExportResult(job, true));

        var result = await _sut.StartCsvExport();

        var accepted = result.Should().BeOfType<AcceptedAtActionResult>().Subject;
        accepted.ActionName.Should().Be(nameof(OrderController.GetCsvExportStatus));
        accepted.Value.Should().Be(job);
    }

    [TestMethod]
    public async Task StartCsvExport_WhenJobAlreadyRunning_ReturnsConflictJob()
    {
        SetUserContext("admin-id", "admin", isAdmin: true);
        var job = new CsvExportJobDto
        {
            Id = Guid.NewGuid(),
            Status = CsvExportJobStatus.Running,
            StartedByUserId = "other-admin",
            FileName = "Orders_Export.csv"
        };
        _sender.Send(new StartOrderCsvExportCommand("admin-id"), Arg.Any<CancellationToken>()).Returns(new StartOrderCsvExportResult(job, false));

        var result = await _sut.StartCsvExport();

        var conflict = result.Should().BeOfType<ObjectResult>().Subject;
        conflict.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        conflict.Value.Should().Be(job);
    }

    [TestMethod]
    public async Task GetCsvExportStatus_WhenFound_ReturnsOk()
    {
        var jobId = Guid.NewGuid();
        var job = new CsvExportJobDto { Id = jobId, Status = CsvExportJobStatus.Completed };
        _sender.Send(new GetOrderCsvExportStatusQuery(jobId), Arg.Any<CancellationToken>()).Returns(job);

        var result = await _sut.GetCsvExportStatus(jobId);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(job);
    }

    [TestMethod]
    public async Task GetCsvExportStatus_WhenMissing_ReturnsNotFound()
    {
        var jobId = Guid.NewGuid();
        _sender.Send(new GetOrderCsvExportStatusQuery(jobId), Arg.Any<CancellationToken>()).Returns((CsvExportJobDto?)null);

        var result = await _sut.GetCsvExportStatus(jobId);

        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task DownloadCsvExport_WhenCompleted_ReturnsCsvFile()
    {
        var jobId = Guid.NewGuid();
        var bytes = "OrderId"u8.ToArray();
        _sender.Send(new GetOrderCsvExportFileQuery(jobId), Arg.Any<CancellationToken>()).Returns(new CsvExportFile(bytes, "Orders_Export.csv"));

        var result = await _sut.DownloadCsvExport(jobId);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileContents.Should().BeEquivalentTo(bytes);
        file.ContentType.Should().Be("text/csv");
        file.FileDownloadName.Should().Be("Orders_Export.csv");
    }

    [TestMethod]
    public async Task DownloadCsvExport_WhenNotReadyOrMissing_ReturnsNotFound()
    {
        var jobId = Guid.NewGuid();
        _sender.Send(new GetOrderCsvExportFileQuery(jobId), Arg.Any<CancellationToken>()).Returns((CsvExportFile?)null);

        var result = await _sut.DownloadCsvExport(jobId);

        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task Create_WhenNullDto_ReturnsBadRequest()
    {
        var result = await _sut.Create(null);

        result.Should().BeOfType<BadRequestResult>();
    }

    [TestMethod]
    public async Task Create_WhenHandlerThrowsNotFound_PropagatesNotFoundException()
    {
        SetUserContext(Guid.NewGuid().ToString(), "user1", false);
        _sender.Send(Arg.Any<CreateOrderCommand>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException(""));

        var act = () => _sut.Create(new OrderSubmitDto());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Create_WhenHandlerThrowsGenericException_PropagatesToGlobalHandler()
    {
        SetUserContext(Guid.NewGuid().ToString(), "user1", false);
        _sender.Send(Arg.Any<CreateOrderCommand>(), Arg.Any<CancellationToken>()).Throws(new Exception("Fail"));

        var act = () => _sut.Create(new OrderSubmitDto());

        await act.Should().ThrowAsync<Exception>().WithMessage("Fail");
    }
}
