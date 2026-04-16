using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Controllers.v1;

[Route("v1/[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderController> _logger;
    private readonly IProductService _productService;
    private readonly IApplicationUserService _applicationUserService;

    public OrderController(IOrderService orderService,
        ILogger<OrderController> logger,
        IProductService productService,
        IApplicationUserService applicationUserService)
    {
        _orderService = orderService;
        _logger = logger;
        _productService = productService;
        _applicationUserService = applicationUserService;
    }

    // GET v1/order
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        _logger.LogInformation("{Controller}: Get all orders (paged) skip={Skip} take={Take}", nameof(OrderController), skip, take);
        var list = await _orderService.GetAll(skip, take);
        var totalCount = await _orderService.GetCount();

        var dtoList = list.Select(MapToDto).ToList();

        return Ok(new PaginationObject<OrderDto>()
        {
            Items = dtoList,
            TotalCount = totalCount
        });
    }

    // GET v1/order/personal
    [Authorize]
    [HttpGet("personal")]
    public async Task<IActionResult> GetPersonal([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        var username = User.Identity!.Name; // non-nullable per auth
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        _logger.LogInformation("{Controller}: Get personal orders for {Username} (paged) skip={Skip} take={Take}", nameof(OrderController), username, skip, take);
        var list = await _orderService.GetPersonal(username, skip, take);
        var totalCount = await _orderService.GetPersonalCount(username);

        var dtoList = list.Select(MapToDto).ToList();

        return Ok(new PaginationObject<OrderDto>()
        {
            Items = dtoList,
            TotalCount = totalCount
        });
    }

    // GET v1/order/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("{Controller}: Get order by id {Id}", nameof(OrderController), id);
        try
        {
            var order = await _orderService.GetOrderById(id);
            return Ok(MapToDto(order));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    // GET v1/order/latest
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest([FromQuery] int take = 6)
    {
        _logger.LogInformation("{Controller}: Get latest {Take} orders", nameof(OrderController), take);
        var list = await _orderService.GetLatestOrders(take);
        var dtoList = list.Select(MapToDto);
        return Ok(dtoList);
    }
    
    // GET v1/order/initialize
    [Authorize]
    [HttpGet("initialize")]
    public async Task<IActionResult> GetOrderInitialize()
    {
        _logger.LogInformation("{Controller}: Initialize order data", nameof(OrderController));
        try
        {
            var products = await _productService.GetAllAvailable();
            var users = await _applicationUserService.GetAllUsers();

            var dto = new OrderInitializeDto()
            {
                Products = products.Select(p => new OrderInitializeProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Stock = p.Stock,
                    Price = p.Price
                }).ToList(),
                Users = users.Select(u => new OrderInitializeUserDto
                {
                    Id = u.Id,
                    FullName = u.FullName
                }).ToList()
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Controller}: Failed to initialize order data", nameof(OrderController));
            return Problem("Failed to initialize order data");
        }
    }

    // POST v1/order
    [Authorize(Roles = "User,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrderSubmitDto? bestelling)
    {
        if (bestelling == null) return BadRequest();

        _logger.LogInformation("{Controller}: Create order - products: {ProductsCount}, users: {UsersCount}", nameof(OrderController), bestelling.Products.Count, bestelling.Users.Count);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");

            // Use new service overload that accepts the API DTO directly along with the acting user's ID
            await _orderService.CreateOrder(bestelling, userId);
            return Ok();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Controller}: Failed to create order", nameof(OrderController));
            return Problem("Failed to create order");
        }
    }

    // GET v1/order/export/csv
    [Authorize(Roles = "Admin")]
    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv()
    {
        _logger.LogInformation("{Controller}: Export orders CSV", nameof(OrderController));
        var csv = await _orderService.ExportOrdersFromPastTwoYearsToCsvAsync();
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "orders_last_2_years.csv");
    }

    // DELETE v1/order/{id}
    // Admins can delete any order; non-admin users can only delete their own orders.
    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        var isAdmin = User.IsInRole("Admin");

        _logger.LogInformation("{Controller}: Delete order {Id} requested by userId {UserId} (admin={IsAdmin})", nameof(OrderController), id, userId, isAdmin);

        if (!isAdmin)
        {
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            try
            {
                var order = await _orderService.GetOrderById(id);
                if (!string.Equals(order.User?.Id, userId, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        try
        {
            await _orderService.DeleteOrder(id);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    private static OrderDto MapToDto(Order order)
    {
        if (order == null) return new OrderDto();

        return new OrderDto
        {
            Id = order.Id,
            CreatedOn = order.CreatedOn,
            ProductId = order.Product?.Id,
            ProductName = order.ProductNaam ?? string.Empty,
            UserId = order.User?.Id != null ? Guid.TryParse(order.User.Id, out var uid) ? uid : (Guid?)null : null,
            UserFullName = order.User?.FullName ?? string.Empty,
            Amount = order.Amount,
            Paid = order.Paid,
            ProfilePictureUrl = order.User?.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{order.User.ProfileImageId}" : null
        };
    }
}
