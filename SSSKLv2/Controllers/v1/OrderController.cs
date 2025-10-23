using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Controllers.v1;

[Route("v1/[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<OrderController> _logger;
    private readonly IProductService _productService;
    private readonly IApplicationUserService _applicationUserService;

    public OrderController(IOrderService orderService,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        ILogger<OrderController> logger,
        IProductService productService,
        IApplicationUserService applicationUserService)
    {
        _orderService = orderService;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _productService = productService;
        _applicationUserService = applicationUserService;
    }

    // GET v1/order
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("{Controller}: Get all orders (queryable)", nameof(OrderController));
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var q = _orderService.GetAllQueryable(context);
        var list = await q.ToListAsync();
        return Ok(list);
    }

    // GET v1/order/personal
    [Authorize]
    [HttpGet("personal")]
    public async Task<IActionResult> GetPersonal()
    {
        var username = User.Identity!.Name; // non-nullable per auth
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        _logger.LogInformation("{Controller}: Get personal orders for {Username}", nameof(OrderController), username);
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var q = _orderService.GetPersonalQueryable(username, context);
        var list = await q.ToListAsync();
        return Ok(list);
    }

    // GET v1/order/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("{Controller}: Get order by id {Id}", nameof(OrderController), id);
        try
        {
            var order = await _orderService.GetOrderById(id);
            return Ok(order);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    // GET v1/order/latest
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest()
    {
        _logger.LogInformation("{Controller}: Get latest orders", nameof(OrderController));
        var list = await _orderService.GetLatestOrders();
        return Ok(list);
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
                    Stock = p.Stock
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
    [Authorize]
    [Authorize(Roles = "User,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrderSubmitDto? bestelling)
    {
        if (bestelling == null) return BadRequest();

        _logger.LogInformation("{Controller}: Create order - products: {ProductsCount}, users: {UsersCount}", nameof(OrderController), bestelling.Products.Count, bestelling.Users.Count);

        try
        {
            // Use new service overload that accepts the API DTO directly
            await _orderService.CreateOrder(bestelling);
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
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("{Controller}: Delete order {Id}", nameof(OrderController), id);
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
}
