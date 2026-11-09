using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKL.Application.Features.Products.Commands.CreateProduct;
using SSSKL.Application.Features.Products.Commands.DeleteProduct;
using SSSKL.Application.Features.Products.Commands.UpdateProduct;
using SSSKL.Application.Features.Products.Queries.GetAllProducts;
using SSSKL.Application.Features.Products.Queries.GetProductById;
using SSSKL.Application.Features.Products.Queries.GetProducts;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Web.Controllers.v1;

[Authorize]
[Route("v1/[controller]")]
[ApiController]
public class ProductController(ISender sender) : ControllerBase
{
    // GET v1/product
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15, [FromQuery] bool all = false)
    {
        if (all)
        {
            return Ok(await sender.Send(new GetAllProductsQuery()));
        }

        return Ok(await sender.Send(new GetProductsQuery(skip, take)));
    }

    // GET v1/product/{id}
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        return Ok(await sender.Send(new GetProductByIdQuery(id)));
    }

    // POST v1/product
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] ProductCreateDto? dto)
    {
        if (dto == null) return BadRequest();

        if (ModelState.Values.SelectMany(v => v.Errors).Any())
        {
            return BadRequest(ModelState);
        }

        var product = await sender.Send(new CreateProductCommand(dto));
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // PUT v1/product/{id}
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProductUpdateDto dto)
    {
        if (ModelState.Values.SelectMany(v => v.Errors).Any())
        {
            return BadRequest(ModelState);
        }

        await sender.Send(new UpdateProductCommand(id, dto));
        return NoContent();
    }

    // DELETE v1/product/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await sender.Send(new DeleteProductCommand(id));
        return NoContent();
    }
}
