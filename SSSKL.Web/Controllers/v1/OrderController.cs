using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKL.Application.Features.Orders.Commands.CreateOrder;
using SSSKL.Application.Features.Orders.Commands.DeleteOrder;
using SSSKL.Application.Features.Orders.Export;
using SSSKL.Application.Features.Orders.Queries.GetLatestOrders;
using SSSKL.Application.Features.Orders.Queries.GetOrderById;
using SSSKL.Application.Features.Orders.Queries.GetOrderInitialize;
using SSSKL.Application.Features.Orders.Queries.GetOrders;
using SSSKL.Application.Features.Orders.Queries.GetPersonalOrders;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Web.Controllers.v1;

[Route("v1/[controller]")]
[ApiController]
public class OrderController(ISender sender) : ControllerBase
{
    // GET v1/order
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        return Ok(await sender.Send(new GetOrdersQuery(skip, take)));
    }

    // GET v1/order/personal
    [Authorize]
    [HttpGet("personal")]
    public async Task<IActionResult> GetPersonal([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        var username = User.Identity!.Name; // non-nullable per auth
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        return Ok(await sender.Send(new GetPersonalOrdersQuery(username, skip, take)));
    }

    // GET v1/order/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Ok(await sender.Send(new GetOrderByIdQuery(id)));
    }

    // GET v1/order/latest
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest([FromQuery] int take = 6)
    {
        return Ok(await sender.Send(new GetLatestOrdersQuery(take)));
    }

    // GET v1/order/initialize
    [Authorize]
    [HttpGet("initialize")]
    public async Task<IActionResult> GetOrderInitialize()
    {
        return Ok(await sender.Send(new GetOrderInitializeQuery()));
    }

    // POST v1/order
    [Authorize(Roles = "User,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrderSubmitDto? bestelling)
    {
        if (bestelling == null) return BadRequest();

        await sender.Send(new CreateOrderCommand(bestelling, GetUserId()));
        return Ok();
    }

    // POST v1/order/export/csv - starts a background job that exports all orders to CSV
    [Authorize(Roles = "Admin")]
    [HttpPost("export/csv")]
    public async Task<IActionResult> StartCsvExport()
    {
        var (job, started) = await sender.Send(new StartOrderCsvExportCommand(GetUserId() ?? string.Empty));
        if (!started)
        {
            return StatusCode(StatusCodes.Status409Conflict, job);
        }

        return AcceptedAtAction(nameof(GetCsvExportStatus), new { jobId = job.Id }, job);
    }

    // GET v1/order/export/csv/{jobId} - status of a specific CSV export job
    [Authorize(Roles = "Admin")]
    [HttpGet("export/csv/{jobId:guid}")]
    public async Task<IActionResult> GetCsvExportStatus(Guid jobId)
    {
        var job = await sender.Send(new GetOrderCsvExportStatusQuery(jobId));
        if (job == null) return NotFound();
        return Ok(job);
    }

    // GET v1/order/export/csv/{jobId}/download - download the generated CSV
    [Authorize(Roles = "Admin")]
    [HttpGet("export/csv/{jobId:guid}/download")]
    public async Task<IActionResult> DownloadCsvExport(Guid jobId)
    {
        var csv = await sender.Send(new GetOrderCsvExportFileQuery(jobId));
        if (csv == null) return NotFound();

        return File(csv.Bytes, "text/csv", csv.FileName);
    }

    // DELETE v1/order/{id}
    // Admins can delete any order; non-admin users can only delete their own orders.
    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        await sender.Send(new DeleteOrderCommand(id, userId, isAdmin));
        return NoContent();
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
}
