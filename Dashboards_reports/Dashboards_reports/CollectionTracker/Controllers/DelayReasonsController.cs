using Dashboards_reports.CollectionTracker.Domain;
using Dashboards_reports.CollectionTracker.Dtos;
using Dashboards_reports.CollectionTracker.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Dashboards_reports.CollectionTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DelayReasonsController(IClientRepository repository) : ControllerBase
{
    private const string DefaultReason = "None";

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DelayReasonDto>>> GetDelayReasons(CancellationToken cancellationToken)
    {
        var items = await repository.GetDelayReasonsAsync(cancellationToken);
        return Ok(items.Select(ToDelayReasonDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DelayReasonDto>> GetDelayReasonById(int id, CancellationToken cancellationToken)
    {
        var item = await repository.GetDelayReasonByIdAsync(id, cancellationToken);
        if (item is null || !item.IsActive)
        {
            return NotFound(new { message = $"Delay reason {id} was not found." });
        }

        return Ok(ToDelayReasonDto(item));
    }

    [HttpPost]
    public async Task<ActionResult<DelayReasonDto>> CreateDelayReason(
        [FromBody] CreateDelayReasonRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Delay reason name is required." });
        }

        if (name.Length > 120)
        {
            return BadRequest(new { message = "Delay reason name must be 120 characters or fewer." });
        }

        var reasons = await repository.GetDelayReasonsAsync(cancellationToken);
        if (reasons.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { message = "Delay reason name already exists." });
        }

        var id = await repository.CreateDelayReasonAsync(name, cancellationToken);
        if (id == 0)
        {
            return Conflict(new { message = "Delay reason name already exists." });
        }

        var created = await repository.GetDelayReasonByIdAsync(id, cancellationToken);
        if (created is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Delay reason created but reload failed." });
        }

        return CreatedAtAction(nameof(GetDelayReasonById), new { id }, ToDelayReasonDto(created));
    }

    [HttpPost("{id:int}/update")]
    public async Task<ActionResult<DelayReasonDto>> UpdateDelayReason(
        int id,
        [FromBody] UpdateDelayReasonRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetDelayReasonByIdAsync(id, cancellationToken);
        if (existing is null || !existing.IsActive)
        {
            return NotFound(new { message = $"Delay reason {id} was not found." });
        }

        if (string.Equals(existing.Name, DefaultReason, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Delay reason 'None' is system-managed and cannot be renamed." });
        }

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Delay reason name is required." });
        }

        if (name.Length > 120)
        {
            return BadRequest(new { message = "Delay reason name must be 120 characters or fewer." });
        }

        var reasons = await repository.GetDelayReasonsAsync(cancellationToken);
        if (reasons.Any(x => x.Id != id && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { message = "Delay reason name already exists." });
        }

        var updated = await repository.UpdateDelayReasonAsync(id, name, cancellationToken);
        if (!updated)
        {
            return NotFound(new { message = $"Delay reason {id} was not found." });
        }

        var refreshed = await repository.GetDelayReasonByIdAsync(id, cancellationToken);
        if (refreshed is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Delay reason updated but reload failed." });
        }

        return Ok(ToDelayReasonDto(refreshed));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteDelayReason(int id, CancellationToken cancellationToken)
    {
        var reason = await repository.GetDelayReasonByIdAsync(id, cancellationToken);
        if (reason is null || !reason.IsActive)
        {
            return NotFound(new { message = $"Delay reason {id} was not found." });
        }

        if (string.Equals(reason.Name, DefaultReason, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Delay reason 'None' is system-managed and cannot be deleted." });
        }

        var fallback = (await repository.GetDelayReasonsAsync(cancellationToken))
            .FirstOrDefault(x => string.Equals(x.Name, DefaultReason, StringComparison.OrdinalIgnoreCase));

        if (fallback is null)
        {
            return BadRequest(new { message = "Default delay reason 'None' is missing." });
        }

        var deleted = await repository.DeleteDelayReasonAsync(
            id,
            reason.Name,
            fallback.Name,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(new { message = $"Delay reason {id} was not found." });
        }

        return NoContent();
    }

    private static DelayReasonDto ToDelayReasonDto(DelayReasonDefinition item) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            SortOrder = item.SortOrder,
            IsActive = item.IsActive
        };
}
