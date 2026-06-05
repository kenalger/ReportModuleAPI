using Dashboards_reports.CollectionTracker.Domain;
using Dashboards_reports.CollectionTracker.Dtos;
using Dashboards_reports.CollectionTracker.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Dashboards_reports.CollectionTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProjectsController(IClientRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> GetProjects(CancellationToken cancellationToken)
    {
        var items = await repository.GetProjectsAsync(cancellationToken);
        return Ok(items.Select(ToProjectDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> GetProjectById(int id, CancellationToken cancellationToken)
    {
        var project = await repository.GetProjectByIdAsync(id, cancellationToken);
        if (project is null || !project.IsActive)
        {
            return NotFound(new { message = $"Project {id} was not found." });
        }

        return Ok(ToProjectDto(project));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateProject(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Project name is required." });
        }

        var existing = await repository.GetProjectsAsync(cancellationToken);
        if (existing.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { message = "Project name already exists." });
        }

        var id = await repository.CreateProjectAsync(name, cancellationToken);
        if (id == 0)
        {
            return Conflict(new { message = "Project name already exists." });
        }

        var created = await repository.GetProjectByIdAsync(id, cancellationToken);
        if (created is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Project created but reload failed." });
        }

        return CreatedAtAction(nameof(GetProjectById), new { id }, ToProjectDto(created));
    }

    [HttpPost("{id:int}/update")]
    public async Task<ActionResult<ProjectDto>> UpdateProject(
        int id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await repository.GetProjectByIdAsync(id, cancellationToken);
        if (project is null || !project.IsActive)
        {
            return NotFound(new { message = $"Project {id} was not found." });
        }

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Project name is required." });
        }

        var allProjects = await repository.GetProjectsAsync(cancellationToken);
        if (allProjects.Any(x => x.Id != id && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { message = "Project name already exists." });
        }

        var updated = await repository.UpdateProjectAsync(id, name, cancellationToken);
        if (!updated)
        {
            return NotFound(new { message = $"Project {id} was not found." });
        }

        var refreshed = await repository.GetProjectByIdAsync(id, cancellationToken);
        return Ok(ToProjectDto(refreshed!));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteProject(int id, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteProjectAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = $"Project {id} was not found." });
        }

        return NoContent();
    }

    // ── Units ──

    [HttpGet("{id:int}/units")]
    public async Task<ActionResult<IReadOnlyList<UnitWithStatusDto>>> GetUnits(int id, CancellationToken cancellationToken)
    {
        var project = await repository.GetProjectByIdAsync(id, cancellationToken);
        if (project is null || !project.IsActive)
        {
            return NotFound(new { message = $"Project {id} was not found." });
        }

        var units = await repository.GetProjectUnitsAsync(id, cancellationToken);
        var clients = await repository.GetClientsAsync(cancellationToken);
        var projectClients = clients.Where(c => c.ProjectId == id).ToList();

        // Build unit → client lookup by UnitId
        var unitClientMap = projectClients
            .Where(c => c.UnitId.HasValue)
            .GroupBy(c => c.UnitId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        // Also match by unit name for clients without UnitId
        var unitNameMap = projectClients
            .Where(c => !c.UnitId.HasValue && !string.IsNullOrEmpty(c.Unit))
            .GroupBy(c => c.Unit, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var result = units.Select(u =>
        {
            Domain.Client? client = null;
            if (unitClientMap.TryGetValue(u.Id, out var byId)) client = byId;
            else if (unitNameMap.TryGetValue(u.Name, out var byName)) client = byName;

            var isCancelled = client != null && Services.ClientMetrics.IsCancelled(client);
            string status = client == null || isCancelled ? "available"
                : Services.ClientMetrics.IsResolved(client) ? "sold"
                : "reserved";

            return new UnitWithStatusDto
            {
                Id = u.Id,
                ProjectId = u.ProjectId,
                Name = u.Name,
                TotalContractPrice = u.TotalContractPrice,
                SortOrder = u.SortOrder,
                IsActive = u.IsActive,
                ClientId = isCancelled ? null : client?.Id,
                ClientName = isCancelled ? null : client?.Name,
                Stage = isCancelled ? null : client?.Stage,
                Status = status
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}/units/{unitId:int}")]
    public async Task<ActionResult<ProjectUnitDto>> GetUnitById(int id, int unitId, CancellationToken cancellationToken)
    {
        var unit = await repository.GetProjectUnitByIdAsync(id, unitId, cancellationToken);
        if (unit is null || !unit.IsActive)
        {
            return NotFound(new { message = $"Unit {unitId} was not found." });
        }

        return Ok(ToUnitDto(unit));
    }

    [HttpPost("{id:int}/units")]
    public async Task<ActionResult<ProjectUnitDto>> CreateUnit(
        int id,
        [FromBody] CreateProjectUnitRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Unit name is required." });
        }

        var unitId = await repository.CreateProjectUnitAsync(id, name, request.TotalContractPrice, cancellationToken);
        if (unitId == 0)
        {
            return Conflict(new { message = "Unit name already exists or project was not found." });
        }

        var created = await repository.GetProjectUnitByIdAsync(id, unitId, cancellationToken);
        if (created is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unit created but reload failed." });
        }

        return CreatedAtAction(nameof(GetUnitById), new { id, unitId }, ToUnitDto(created));
    }

    [HttpPost("{id:int}/units/{unitId:int}/update")]
    public async Task<ActionResult<ProjectUnitDto>> UpdateUnit(
        int id,
        int unitId,
        [FromBody] UpdateProjectUnitRequest request,
        CancellationToken cancellationToken)
    {
        var unit = await repository.GetProjectUnitByIdAsync(id, unitId, cancellationToken);
        if (unit is null || !unit.IsActive)
        {
            return NotFound(new { message = $"Unit {unitId} was not found." });
        }

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Unit name is required." });
        }

        var updated = await repository.UpdateProjectUnitAsync(id, unitId, name, request.TotalContractPrice, cancellationToken);
        if (!updated)
        {
            return NotFound(new { message = $"Unit {unitId} was not found." });
        }

        var refreshed = await repository.GetProjectUnitByIdAsync(id, unitId, cancellationToken);
        return Ok(ToUnitDto(refreshed!));
    }

    [HttpPost("{id:int}/units/{unitId:int}/delete")]
    public async Task<IActionResult> DeleteUnit(int id, int unitId, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteProjectUnitAsync(id, unitId, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = $"Unit {unitId} was not found." });
        }

        return NoContent();
    }

    private static ProjectDto ToProjectDto(Project project) =>
        new()
        {
            Id = project.Id,
            Name = project.Name,
            SortOrder = project.SortOrder,
            IsActive = project.IsActive
        };

    private static ProjectUnitDto ToUnitDto(ProjectUnit unit) =>
        new()
        {
            Id = unit.Id,
            ProjectId = unit.ProjectId,
            Name = unit.Name,
            TotalContractPrice = unit.TotalContractPrice,
            SortOrder = unit.SortOrder,
            IsActive = unit.IsActive
        };
}
