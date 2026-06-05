using Dashboards_reports.CollectionTracker.Domain;
using Dashboards_reports.CollectionTracker.Dtos;
using Dashboards_reports.CollectionTracker.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Dashboards_reports.CollectionTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StagesController(IClientRepository repository) : ControllerBase
{
    private static readonly HashSet<string> ProtectedStages =
    [
        "Resolved",
        "Proceeds Released",
        "Cancellation"
    ];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StageDto>>> GetStages(CancellationToken cancellationToken)
    {
        var items = await repository.GetStagesAsync(cancellationToken);
        return Ok(items.Select(ToStageDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StageDto>> GetStageById(int id, CancellationToken cancellationToken)
    {
        var stage = await repository.GetStageByIdAsync(id, cancellationToken);
        if (stage is null || !stage.IsActive)
        {
            return NotFound(new { message = $"Stage {id} was not found." });
        }

        return Ok(ToStageDto(stage));
    }

    [HttpPost]
    public async Task<ActionResult<StageDto>> CreateStage(
        [FromBody] CreateStageRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Stage name is required." });
        }

        if (name.Length > 80)
        {
            return BadRequest(new { message = "Stage name must be 80 characters or fewer." });
        }

        var existing = await repository.GetStagesAsync(cancellationToken);
        if (existing.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { message = "Stage name already exists." });
        }

        var id = await repository.CreateStageAsync(name, cancellationToken);
        if (id == 0)
        {
            return Conflict(new { message = "Stage name already exists." });
        }

        var created = await repository.GetStageByIdAsync(id, cancellationToken);
        if (created is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Stage created but reload failed." });
        }

        return CreatedAtAction(nameof(GetStageById), new { id }, ToStageDto(created));
    }

    [HttpPost("{id:int}/update")]
    public async Task<ActionResult<StageDto>> UpdateStage(
        int id,
        [FromBody] UpdateStageRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetStageByIdAsync(id, cancellationToken);
        if (existing is null || !existing.IsActive)
        {
            return NotFound(new { message = $"Stage {id} was not found." });
        }

        if (ProtectedStages.Contains(existing.Name))
        {
            return BadRequest(new { message = $"Stage '{existing.Name}' is system-managed and cannot be renamed." });
        }

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Stage name is required." });
        }

        if (name.Length > 80)
        {
            return BadRequest(new { message = "Stage name must be 80 characters or fewer." });
        }

        var allStages = await repository.GetStagesAsync(cancellationToken);
        if (allStages.Any(x => x.Id != id && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { message = "Stage name already exists." });
        }

        var updated = await repository.UpdateStageAsync(id, name, cancellationToken);
        if (!updated)
        {
            return NotFound(new { message = $"Stage {id} was not found." });
        }

        var refreshed = await repository.GetStageByIdAsync(id, cancellationToken);
        if (refreshed is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Stage updated but reload failed." });
        }

        return Ok(ToStageDto(refreshed));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteStage(int id, CancellationToken cancellationToken)
    {
        var stage = await repository.GetStageByIdAsync(id, cancellationToken);
        if (stage is null || !stage.IsActive)
        {
            return NotFound(new { message = $"Stage {id} was not found." });
        }

        if (ProtectedStages.Contains(stage.Name))
        {
            return BadRequest(new { message = $"Stage '{stage.Name}' is system-managed and cannot be deleted." });
        }

        var activeStages = (await repository.GetStagesAsync(cancellationToken)).ToList();
        var remainingStages = activeStages
            .Where(s => s.Id != stage.Id)
            .OrderBy(s => s.SortOrder)
            .ToList();

        var isUsed = await repository.IsStageUsedAsync(stage.Name, cancellationToken);
        string? replacementStageName = null;
        if (isUsed)
        {
            if (remainingStages.Count == 0)
            {
                return BadRequest(new { message = "Cannot delete a stage that is in use when no replacement stage exists." });
            }

            var previousStage = remainingStages
                .Where(s => s.SortOrder < stage.SortOrder)
                .OrderByDescending(s => s.SortOrder)
                .FirstOrDefault();

            replacementStageName = previousStage?.Name ?? remainingStages[0].Name;
        }

        var deleted = await repository.DeleteStageAsync(id, stage.Name, replacementStageName, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = $"Stage {id} was not found." });
        }

        return NoContent();
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderStages(
        [FromBody] ReorderStagesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StageIds.Count == 0)
            return BadRequest(new { message = "StageIds list is required." });

        await repository.ReorderStagesAsync(request.StageIds, cancellationToken);
        return NoContent();
    }

    private static StageDto ToStageDto(StageDefinition stage) =>
        new()
        {
            Id = stage.Id,
            Name = stage.Name,
            SortOrder = stage.SortOrder,
            IsActive = stage.IsActive
        };
}
