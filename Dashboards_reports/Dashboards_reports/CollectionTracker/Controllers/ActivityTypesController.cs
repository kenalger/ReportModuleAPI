using Dashboards_reports.CollectionTracker.Domain;
using Dashboards_reports.CollectionTracker.Dtos;
using Dashboards_reports.CollectionTracker.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Dashboards_reports.CollectionTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ActivityTypesController(IClientRepository repository) : ControllerBase
{
    private static readonly HashSet<string> ProtectedCodes =
    [
        "note",
        "system"
    ];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ActivityTypeDto>>> GetActivityTypes(CancellationToken cancellationToken)
    {
        var items = await repository.GetActivityTypesAsync(cancellationToken);
        return Ok(items.Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ActivityTypeDto>> GetActivityTypeById(int id, CancellationToken cancellationToken)
    {
        var item = await repository.GetActivityTypeByIdAsync(id, cancellationToken);
        if (item is null || !item.IsActive)
        {
            return NotFound(new { message = $"Activity type {id} was not found." });
        }

        return Ok(ToDto(item));
    }

    [HttpPost]
    public async Task<ActionResult<ActivityTypeDto>> CreateActivityType(
        [FromBody] CreateActivityTypeRequest request,
        CancellationToken cancellationToken)
    {
        var code = request.Code?.Trim().ToLowerInvariant();
        var label = request.Label?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new { message = "Activity type code is required." });
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return BadRequest(new { message = "Activity type label is required." });
        }

        if (code.Length > 30)
        {
            return BadRequest(new { message = "Activity type code must be 30 characters or fewer." });
        }

        if (label.Length > 50)
        {
            return BadRequest(new { message = "Activity type label must be 50 characters or fewer." });
        }

        var existing = await repository.GetActivityTypesAsync(cancellationToken);
        if (existing.Any(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { message = "Activity type code already exists." });
        }

        var id = await repository.CreateActivityTypeAsync(code, label, cancellationToken);
        if (id == 0)
        {
            return Conflict(new { message = "Activity type code already exists." });
        }

        var created = await repository.GetActivityTypeByIdAsync(id, cancellationToken);
        if (created is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Activity type created but reload failed." });
        }

        return CreatedAtAction(nameof(GetActivityTypeById), new { id }, ToDto(created));
    }

    [HttpPost("{id:int}/update")]
    public async Task<ActionResult<ActivityTypeDto>> UpdateActivityType(
        int id,
        [FromBody] UpdateActivityTypeRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetActivityTypeByIdAsync(id, cancellationToken);
        if (existing is null || !existing.IsActive)
        {
            return NotFound(new { message = $"Activity type {id} was not found." });
        }

        if (ProtectedCodes.Contains(existing.Code))
        {
            return BadRequest(new { message = $"Activity type '{existing.Code}' is system-managed and cannot be renamed." });
        }

        var code = request.Code?.Trim().ToLowerInvariant();
        var label = request.Label?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new { message = "Activity type code is required." });
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return BadRequest(new { message = "Activity type label is required." });
        }

        if (code.Length > 30)
        {
            return BadRequest(new { message = "Activity type code must be 30 characters or fewer." });
        }

        if (label.Length > 50)
        {
            return BadRequest(new { message = "Activity type label must be 50 characters or fewer." });
        }

        var all = await repository.GetActivityTypesAsync(cancellationToken);
        if (all.Any(x => x.Id != id && string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { message = "Activity type code already exists." });
        }

        var updated = await repository.UpdateActivityTypeAsync(id, code, label, cancellationToken);
        if (!updated)
        {
            return NotFound(new { message = $"Activity type {id} was not found." });
        }

        var refreshed = await repository.GetActivityTypeByIdAsync(id, cancellationToken);
        if (refreshed is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Activity type updated but reload failed." });
        }

        return Ok(ToDto(refreshed));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteActivityType(int id, CancellationToken cancellationToken)
    {
        var item = await repository.GetActivityTypeByIdAsync(id, cancellationToken);
        if (item is null || !item.IsActive)
        {
            return NotFound(new { message = $"Activity type {id} was not found." });
        }

        if (ProtectedCodes.Contains(item.Code))
        {
            return BadRequest(new { message = $"Activity type '{item.Code}' is system-managed and cannot be deleted." });
        }

        var fallback = (await repository.GetActivityTypesAsync(cancellationToken))
            .FirstOrDefault(x => string.Equals(x.Code, "note", StringComparison.OrdinalIgnoreCase));
        if (fallback is null)
        {
            return BadRequest(new { message = "Default activity type 'note' is missing." });
        }

        var deleted = await repository.DeleteActivityTypeAsync(id, item.Code, fallback.Code, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = $"Activity type {id} was not found." });
        }

        return NoContent();
    }

    private static ActivityTypeDto ToDto(ActivityTypeDefinition item) =>
        new()
        {
            Id = item.Id,
            Code = item.Code,
            Label = item.Label,
            SortOrder = item.SortOrder,
            IsActive = item.IsActive
        };
}
