using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Controllers.v1;

[Authorize]
[Route("v1/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductController> _logger;

    public ProductController(IProductService productService, ILogger<ProductController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    // GET v1/product
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        _logger.LogInformation("{Controller}: Get all products skip={Skip} take={Take}", nameof(ProductController), skip, take);
        var products = await _productService.GetAll(skip, take);
        var totalCount = await _productService.GetCount();
        
        return Ok(new PaginationObject<ProductDto>()
        {
            Items = products.Select(MapToDto),
            TotalCount = totalCount
        });
    }

    // GET v1/product/{id}
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        _logger.LogInformation("{Controller}: Get product by id {Id}", nameof(ProductController), id);
        try
        {
            var product = await _productService.GetProductById(id);
            return Ok(MapToDto(product));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    // POST v1/product
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] ProductCreateDto? dto)
    {
        if (dto == null) return BadRequest();

        _logger.LogInformation("{Controller}: Create product with name {Name}", nameof(ProductController), dto.Name);
        
        if (ModelState.Values.SelectMany(v => v.Errors).Any())
        {
            return BadRequest(ModelState);
        }

        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Stock = dto.Stock
        };

        try
        {
            await _productService.CreateProduct(product);
            var outDto = MapToDto(product);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, outDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Controller}: Failed to create product", nameof(ProductController));
            return Problem("Failed to create product");
        }
    }

    // PUT v1/product/{id}
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProductUpdateDto dto)
    {
        _logger.LogInformation("{Controller}: Update product {Id}", nameof(ProductController), id);
        
        if (ModelState.Values.SelectMany(v => v.Errors).Any())
        {
            return BadRequest(ModelState);
        }

        try
        {
            var existing = await _productService.GetProductById(id);
            if (existing == null) return NotFound();

            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.Price = dto.Price;
            existing.Stock = dto.Stock;

            await _productService.UpdateProduct(existing);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Controller}: Failed to update product", nameof(ProductController));
            return Problem("Failed to update product");
        }
    }

    // DELETE v1/product/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("{Controller}: Delete product {Id}", nameof(ProductController), id);
        try
        {
            var existing = await _productService.GetProductById(id);
            if (existing == null) return NotFound();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        await _productService.DeleteProduct(id);
        return NoContent();
    }

    private static ProductDto MapToDto(Product p) => new ProductDto
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        Stock = p.Stock
    };
}