using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Data;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Controllers.v1;

[Route("v1/[controller]")]
[ApiController]
public class TopUpController : ControllerBase
{
    private readonly ITopUpService _topUpService;
    private readonly ILogger<TopUpController> _logger;

    public TopUpController(ITopUpService topUpService, ILogger<TopUpController> logger)
    {
        _topUpService = topUpService;
        _logger = logger;
    }

    // GET v1/topup
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TopUpDto>>> GetAll([FromServices] ApplicationDbContext dbContext)
    {
        _logger.LogInformation("{Controller}: Get all topups", nameof(TopUpController));
        var query = _topUpService.GetAllQueryable(dbContext);
        var list = await Task.FromResult(query.ToList());
        var dto = list.Select(MapToDto).ToList();
        return Ok(dto);
    }

    // GET v1/topup/personal/{username}
    [HttpGet("personal/{username}")]
    public async Task<ActionResult<IEnumerable<TopUpDto>>> GetPersonal(string username, [FromServices] ApplicationDbContext dbContext)
    {
        _logger.LogInformation("{Controller}: Get personal topups for {Username}", nameof(TopUpController), username);
        var query = _topUpService.GetPersonalQueryable(username, dbContext);
        var list = await Task.FromResult(query.ToList());
        var dto = list.Select(MapToDto).ToList();
        return Ok(dto);
    }

    // GET v1/topup/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<TopUpDto>> GetById(string id)
    {
        _logger.LogInformation("{Controller}: Get topup by id {Id}", nameof(TopUpController), id);
        try
        {
            var topup = await _topUpService.GetById(id);
            return Ok(MapToDto(topup));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Controller}: TopUp not found {Id}", nameof(TopUpController), id);
            return NotFound();
        }
    }

    // POST v1/topup
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<TopUpDto>> Create([FromBody] TopUpCreateDto? dto)
    {
        if (dto == null) return BadRequest();

        _logger.LogInformation("{Controller}: Create topup for user {UserName}", nameof(TopUpController), dto.UserName);

        if (ModelState.Values.SelectMany(v => v.Errors).Any())
        {
            return BadRequest(ModelState);
        }

        // Map DTO to model
        var topup = new TopUp
        {
            User = new ApplicationUser { UserName = dto.UserName },
            Saldo = dto.Saldo
        };

        try
        {
            await _topUpService.CreateTopUp(topup);
            var outDto = MapToDto(topup);
            return CreatedAtAction(nameof(GetById), new { id = topup.Id }, outDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Controller}: Failed to create topup", nameof(TopUpController));
            return Problem("Failed to create topup");
        }
    }

    // DELETE v1/topup/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("{Controller}: Delete topup {Id}", nameof(TopUpController), id);
        try
        {
            // Ensure exists
            var existing = await _topUpService.GetById(id.ToString());
            if (existing == null) return NotFound();
        }
        catch (Exception)
        {
            return NotFound();
        }

        await _topUpService.DeleteTopUp(id);
        return NoContent();
    }

    private static TopUpDto MapToDto(TopUp t) => new TopUpDto
    {
        Id = t.Id,
        UserName = t.User?.UserName,
        Saldo = t.Saldo
    };
}