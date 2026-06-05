using Dashboards_reports.CollectionTracker.Domain;
using Dashboards_reports.CollectionTracker.Dtos;
using Dashboards_reports.CollectionTracker.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Dashboards_reports.CollectionTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class FinancingTypesController(IClientRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FinancingTypeDto>>> GetFinancingTypes(CancellationToken cancellationToken)
    {
        var items = await repository.GetFinancingTypesAsync(cancellationToken);
        return Ok(items.Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FinancingTypeDto>> GetFinancingTypeById(int id, CancellationToken cancellationToken)
    {
        var item = await repository.GetFinancingTypeByIdAsync(id, cancellationToken);
        if (item is null || !item.IsActive)
        {
            return NotFound(new { message = $"Financing type {id} was not found." });
        }

        return Ok(ToDto(item));
    }

    [HttpPost]
    public async Task<ActionResult<FinancingTypeDto>> CreateFinancingType(
        [FromBody] CreateFinancingTypeRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Financing type name is required." });
        }

        if (name.Length > 60)
        {
            return BadRequest(new { message = "Financing type name must be 60 characters or fewer." });
        }

        var existing = await repository.GetFinancingTypesAsync(cancellationToken);
        if (existing.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { message = "Financing type name already exists." });
        }

        var id = await repository.CreateFinancingTypeAsync(name, cancellationToken);
        if (id == 0)
        {
            return Conflict(new { message = "Financing type name already exists." });
        }

        var created = await repository.GetFinancingTypeByIdAsync(id, cancellationToken);
        if (created is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Financing type created but reload failed." });
        }

        return CreatedAtAction(nameof(GetFinancingTypeById), new { id }, ToDto(created));
    }

    [HttpPost("{id:int}/update")]
    public async Task<ActionResult<FinancingTypeDto>> UpdateFinancingType(
        int id,
        [FromBody] UpdateFinancingTypeRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetFinancingTypeByIdAsync(id, cancellationToken);
        if (existing is null || !existing.IsActive)
        {
            return NotFound(new { message = $"Financing type {id} was not found." });
        }

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Financing type name is required." });
        }

        if (name.Length > 60)
        {
            return BadRequest(new { message = "Financing type name must be 60 characters or fewer." });
        }

        var all = await repository.GetFinancingTypesAsync(cancellationToken);
        if (all.Any(x => x.Id != id && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { message = "Financing type name already exists." });
        }

        var updated = await repository.UpdateFinancingTypeAsync(id, name, cancellationToken);
        if (!updated)
        {
            return NotFound(new { message = $"Financing type {id} was not found." });
        }

        var refreshed = await repository.GetFinancingTypeByIdAsync(id, cancellationToken);
        if (refreshed is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Financing type updated but reload failed." });
        }

        return Ok(ToDto(refreshed));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteFinancingType(int id, CancellationToken cancellationToken)
    {
        var item = await repository.GetFinancingTypeByIdAsync(id, cancellationToken);
        if (item is null || !item.IsActive)
        {
            return NotFound(new { message = $"Financing type {id} was not found." });
        }

        var all = (await repository.GetFinancingTypesAsync(cancellationToken))
            .OrderBy(x => x.SortOrder)
            .ToList();
        var remaining = all.Where(x => x.Id != id).ToList();
        if (remaining.Count == 0)
        {
            return BadRequest(new { message = "At least one financing type must remain." });
        }

        var previous = remaining
            .Where(x => x.SortOrder < item.SortOrder)
            .OrderByDescending(x => x.SortOrder)
            .FirstOrDefault();
        var replacement = previous?.Name ?? remaining[0].Name;

        var deleted = await repository.DeleteFinancingTypeAsync(id, item.Name, replacement, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = $"Financing type {id} was not found." });
        }

        return NoContent();
    }

    private static FinancingTypeDto ToDto(FinancingTypeDefinition item) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            SortOrder = item.SortOrder,
            IsActive = item.IsActive
        };
}
