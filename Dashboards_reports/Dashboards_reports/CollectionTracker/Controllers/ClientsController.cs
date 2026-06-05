using Dashboards_reports.CollectionTracker.Domain;
using Dashboards_reports.CollectionTracker.Dtos;
using Dashboards_reports.CollectionTracker.Repositories;
using Dashboards_reports.CollectionTracker.Services;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable once RedundantUsingDirective
using UserCtx = Dashboards_reports.CollectionTracker.Services.UserContext;

namespace Dashboards_reports.CollectionTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ClientsController(IClientRepository repository) : ControllerBase
{
    private static readonly HashSet<string> ValidTaskPriority =
    [
        "high",
        "medium",
        "low"
    ];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClientListItemDto>>> GetClients(
        [FromQuery] ClientQueryParams query,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var stageOrder = (await repository.GetStagesAsync(cancellationToken))
            .Select(s => s.Name)
            .ToList();
        var clients = await repository.GetClientsAsync(cancellationToken);
        if (query.ProjectId.HasValue)
            clients = clients.Where(c => c.ProjectId == query.ProjectId.Value).ToList();
        var mapped = clients.Select(client => ClientMapper.ToListItem(client, today, stageOrder)).ToList();

        mapped = ApplyFilter(mapped, query.Filter);
        mapped = ApplySearch(mapped, query.Search);
        mapped = ApplySort(mapped, query.SortField, query.SortDir, stageOrder);

        return Ok(mapped);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClientDetailDto>> GetClientById(int id, CancellationToken cancellationToken)
    {
        var client = await repository.GetClientByIdAsync(id, cancellationToken);
        if (client is null)
        {
            return NotFound(new { message = $"Client {id} was not found." });
        }

        var stageOrder = (await repository.GetStagesAsync(cancellationToken))
            .Select(s => s.Name)
            .ToList();

        return Ok(ClientMapper.ToDetail(client, DateTime.UtcNow.Date, stageOrder));
    }

    [HttpPost]
    public async Task<ActionResult<ClientDetailDto>> CreateClient(
        [FromBody] UpsertClientRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateClientRequestAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var userName = UserCtx.GetUserDisplayName(HttpContext);
        var resolved = await ResolveUnitAsync(request, cancellationToken);
        var normalized = NormalizeClientRequest(resolved, DateTime.UtcNow.Date, null);
        normalized = normalized with { CreatedBy = userName, ModifiedBy = userName };
        var id = await repository.CreateClientAsync(normalized, cancellationToken);
        var client = await repository.GetClientByIdAsync(id, cancellationToken);

        if (client is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Client created but reload failed." });
        }

        var stageOrder = (await repository.GetStagesAsync(cancellationToken))
            .Select(s => s.Name)
            .ToList();

        return CreatedAtAction(nameof(GetClientById), new { id }, ClientMapper.ToDetail(client, DateTime.UtcNow.Date, stageOrder));
    }

    [HttpPost("{id:int}/update")]
    public async Task<ActionResult<ClientDetailDto>> UpdateClient(
        int id,
        [FromBody] UpsertClientRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateClientRequestAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var existing = await repository.GetClientByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound(new { message = $"Client {id} was not found." });
        }

        var userName = UserCtx.GetUserDisplayName(HttpContext);
        var resolved = await ResolveUnitAsync(request, cancellationToken);
        var normalized = NormalizeClientRequest(resolved, DateTime.UtcNow.Date, existing);
        normalized = normalized with { ModifiedBy = userName };
        var updated = await repository.UpdateClientAsync(id, normalized, cancellationToken);
        if (!updated)
        {
            return NotFound(new { message = $"Client {id} was not found." });
        }

        var refreshed = await repository.GetClientByIdAsync(id, cancellationToken);
        var stageOrder = (await repository.GetStagesAsync(cancellationToken))
            .Select(s => s.Name)
            .ToList();
        return Ok(ClientMapper.ToDetail(refreshed!, DateTime.UtcNow.Date, stageOrder));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteClient(int id, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteClientAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = $"Client {id} was not found." });
        }

        return NoContent();
    }

    [HttpPost("{id:int}/resolve")]
    public async Task<IActionResult> ResolveClient(
        int id,
        [FromBody] ResolveClientRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ResolvedHow))
        {
            return BadRequest(new { message = "ResolvedHow is required." });
        }

        var userName = UserCtx.GetUserDisplayName(HttpContext);
        var resolved = await repository.ResolveClientAsync(
            id,
            request with
            {
                ResolvedHow = request.ResolvedHow.Trim(),
                ResolvedNotes = string.IsNullOrWhiteSpace(request.ResolvedNotes) ? null : request.ResolvedNotes.Trim(),
                ResolvedDate = request.ResolvedDate?.Date
            },
            userName,
            cancellationToken);

        if (!resolved)
        {
            return NotFound(new { message = $"Client {id} was not found." });
        }

        return NoContent();
    }

    [HttpGet("{id:int}/activities")]
    public async Task<ActionResult<IReadOnlyList<ActivityDto>>> GetActivities(int id, CancellationToken cancellationToken)
    {
        var client = await repository.GetClientByIdAsync(id, cancellationToken);
        if (client is null)
        {
            return NotFound(new { message = $"Client {id} was not found." });
        }

        var activities = client.Activities
            .OrderByDescending(a => a.ActivityDateTime)
            .Select(ToActivityDto)
            .ToList();

        return Ok(activities);
    }

    [HttpPost("{id:int}/activities")]
    public async Task<ActionResult<ActivityDto>> AddActivity(
        int id,
        [FromBody] AddActivityRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { message = "Description is required." });
        }

        var activityType = request.ActivityType.Trim().ToLowerInvariant();
        var validActivityTypes = await repository.GetActivityTypesAsync(cancellationToken);
        var isSupportedType = validActivityTypes.Any(t => string.Equals(t.Code, activityType, StringComparison.OrdinalIgnoreCase));
        if (!isSupportedType)
        {
            return BadRequest(new { message = "Unsupported activity type." });
        }

        var userName = UserCtx.GetUserDisplayName(HttpContext);
        var activityId = await repository.AddActivityAsync(
            id,
            request with { ActivityType = activityType, Description = request.Description.Trim() },
            userName,
            cancellationToken);

        if (activityId == 0)
        {
            return NotFound(new { message = $"Client {id} was not found." });
        }

        var client = await repository.GetClientByIdAsync(id, cancellationToken);
        var activity = client?.Activities.FirstOrDefault(a => a.Id == activityId);

        if (activity is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Activity created but reload failed." });
        }

        return CreatedAtAction(nameof(GetActivities), new { id }, ToActivityDto(activity));
    }

    [HttpPost("{id:int}/activities/{activityId:int}/delete")]
    public async Task<IActionResult> DeleteActivity(int id, int activityId, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteActivityAsync(id, activityId, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = $"Activity {activityId} for client {id} was not found." });
        }

        return NoContent();
    }

    [HttpGet("{id:int}/tasks")]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetTasks(int id, CancellationToken cancellationToken)
    {
        var client = await repository.GetClientByIdAsync(id, cancellationToken);
        if (client is null)
        {
            return NotFound(new { message = $"Client {id} was not found." });
        }

        var tasks = client.Tasks
            .OrderBy(t => t.IsDone)
            .ThenBy(t => t.DueDate.HasValue ? 0 : 1)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => ClientMetrics.PriorityRank(t.Priority))
            .Select(ToTaskDto)
            .ToList();

        return Ok(tasks);
    }

    [HttpPost("{id:int}/tasks")]
    public async Task<ActionResult<TaskDto>> AddTask(
        int id,
        [FromBody] AddTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { message = "Description is required." });
        }

        var priority = request.Priority.Trim().ToLowerInvariant();
        if (!ValidTaskPriority.Contains(priority))
        {
            return BadRequest(new { message = "Task priority must be high, medium, or low." });
        }

        var taskId = await repository.AddTaskAsync(
            id,
            request with
            {
                Description = request.Description.Trim(),
                Priority = priority,
                AssignedTo = string.IsNullOrWhiteSpace(request.AssignedTo) ? null : request.AssignedTo.Trim(),
                DueDate = request.DueDate?.Date
            },
            cancellationToken);

        if (taskId == 0)
        {
            return NotFound(new { message = $"Client {id} was not found." });
        }

        var client = await repository.GetClientByIdAsync(id, cancellationToken);
        var task = client?.Tasks.FirstOrDefault(t => t.Id == taskId);

        if (task is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Task created but reload failed." });
        }

        return CreatedAtAction(nameof(GetTasks), new { id }, ToTaskDto(task));
    }

    [HttpPost("{id:int}/tasks/{taskId:int}/status")]
    public async Task<IActionResult> UpdateTaskStatus(
        int id,
        int taskId,
        [FromBody] UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await repository.UpdateTaskStatusAsync(id, taskId, request.IsDone, cancellationToken);
        if (!updated)
        {
            return NotFound(new { message = $"Task {taskId} for client {id} was not found." });
        }

        return NoContent();
    }

    [HttpPost("{id:int}/tasks/{taskId:int}/delete")]
    public async Task<IActionResult> DeleteTask(int id, int taskId, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteTaskAsync(id, taskId, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = $"Task {taskId} for client {id} was not found." });
        }

        return NoContent();
    }

    private async Task<UpsertClientRequest> ResolveUnitAsync(UpsertClientRequest request, CancellationToken cancellationToken)
    {
        if (!request.UnitId.HasValue)
        {
            return request;
        }

        var unit = await repository.GetProjectUnitByUnitIdAsync(request.UnitId.Value, cancellationToken);
        if (unit is null)
        {
            return request;
        }

        return request with
        {
            Unit = unit.Name,
            TotalContractPrice = unit.TotalContractPrice
        };
    }

    private async Task<string?> ValidateClientRequestAsync(UpsertClientRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Name is required.";
        }

        if (!request.UnitId.HasValue && string.IsNullOrWhiteSpace(request.Unit))
        {
            return "Unit is required.";
        }

        if (request.UnitId.HasValue)
        {
            var unit = await repository.GetProjectUnitByUnitIdAsync(request.UnitId.Value, cancellationToken);
            if (unit is null)
            {
                return "Invalid unit.";
            }
        }

        var financing = request.FinancingType?.Trim();
        if (string.IsNullOrWhiteSpace(financing))
        {
            return "Invalid financing type.";
        }

        var financingTypes = await repository.GetFinancingTypesAsync(cancellationToken);
        var validFinancing = financingTypes.Any(f => string.Equals(f.Name, financing, StringComparison.OrdinalIgnoreCase));
        if (!validFinancing)
        {
            return "Invalid financing type.";
        }

        var stage = request.Stage?.Trim();
        if (string.IsNullOrWhiteSpace(stage))
        {
            return "Invalid stage.";
        }

        var stages = await repository.GetStagesAsync(cancellationToken);
        var validStage = stages.Any(s => string.Equals(s.Name, stage, StringComparison.OrdinalIgnoreCase));
        if (!validStage)
        {
            return "Invalid stage.";
        }

        var delayReasons = await repository.GetDelayReasonsAsync(cancellationToken);
        var normalizedDelayReasons = NormalizeDelayReasonsInput(request);
        foreach (var reason in normalizedDelayReasons)
        {
            var validReason = delayReasons.Any(d => string.Equals(d.Name, reason, StringComparison.OrdinalIgnoreCase));
            if (!validReason)
            {
                return "Invalid delay reason.";
            }
        }

        return null;
    }

    private static UpsertClientRequest NormalizeClientRequest(
        UpsertClientRequest request,
        DateTime today,
        Client? existingClient)
    {
        var stage = request.Stage.Trim();
        var resolvedDate = request.ResolvedDate?.Date;
        var delayReasons = NormalizeDelayReasonsInput(request);
        var primaryDelayReason = delayReasons.FirstOrDefault() ?? "None";
        var secondaryDelayReason = delayReasons.Skip(1).FirstOrDefault();

        if ((string.Equals(stage, "Resolved", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(stage, "Proceeds Released", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(stage, "Cancellation", StringComparison.OrdinalIgnoreCase)) &&
            !resolvedDate.HasValue)
        {
            var wasAlreadyTerminal = existingClient is not null &&
                (ClientMetrics.IsResolved(existingClient) || ClientMetrics.IsCancelled(existingClient));
            resolvedDate = wasAlreadyTerminal ? existingClient?.ResolvedDate?.Date : today.Date;
        }

        return request with
        {
            Name = request.Name.Trim(),
            Unit = request.Unit.Trim(),
            ContactNumber = string.IsNullOrWhiteSpace(request.ContactNumber) ? null : request.ContactNumber.Trim(),
            BrokerName = string.IsNullOrWhiteSpace(request.BrokerName) ? null : request.BrokerName.Trim(),
            FinancingType = request.FinancingType.Trim(),
            Stage = stage,
            StageDate = request.StageDate?.Date ?? today.Date,
            TargetDate = request.TargetDate?.Date,
            ResolvedDate = resolvedDate,
            DelayReasons = delayReasons,
            DelayReason = primaryDelayReason,
            SecondaryDelayReason = secondaryDelayReason,
            NextAction = string.IsNullOrWhiteSpace(request.NextAction) ? null : request.NextAction.Trim(),
            FollowUpDate = request.FollowUpDate?.Date,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };
    }

    private static IReadOnlyList<string> NormalizeDelayReasonsInput(UpsertClientRequest request)
    {
        var requested = (request.DelayReasons ?? [])
            .Where(reason => !string.IsNullOrWhiteSpace(reason))
            .Select(reason => reason.Trim())
            .Where(reason => !string.Equals(reason, "None", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requested.Count > 0)
        {
            return requested;
        }

        var fromLegacyFields = new[] { request.DelayReason, request.SecondaryDelayReason }
            .Where(reason => !string.IsNullOrWhiteSpace(reason))
            .Select(reason => reason!.Trim())
            .Where(reason => !string.Equals(reason, "None", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return fromLegacyFields;
    }

    private static List<ClientListItemDto> ApplyFilter(List<ClientListItemDto> clients, string? filter)
    {
        var normalized = filter?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "critical" => clients.Where(c => c.AgingStatus == "critical").ToList(),
            "warning" => clients.Where(c => c.AgingStatus == "warning").ToList(),
            "watch" => clients.Where(c => c.AgingStatus == "watch").ToList(),
            "ok" => clients.Where(c => c.AgingStatus == "ok" && !c.IsResolved && !c.IsCancelled).ToList(),
            "resolved" => clients.Where(c => c.IsResolved).ToList(),
            "cancellation" => clients.Where(c => c.IsCancelled).ToList(),
            _ => clients
        };
    }

    private static List<ClientListItemDto> ApplySearch(List<ClientListItemDto> clients, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return clients;
        }

        var q = search.Trim();
        return clients
            .Where(c =>
                c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.Unit.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (c.BrokerName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }

    private static List<ClientListItemDto> ApplySort(
        List<ClientListItemDto> clients,
        string? sortField,
        string? sortDir,
        IReadOnlyList<string> stageOrder)
    {
        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var field = sortField?.Trim().ToLowerInvariant();

        Func<ClientListItemDto, object> selector = field switch
        {
            "name" => c => c.Name,
            "stage" => c => ClientMetrics.StageRank(c.Stage, stageOrder),
            "aging" => c => c.DaysInStage,
            "targetdate" => c => c.TargetDate ?? DateTime.MaxValue,
            _ => c => c.Name
        };

        return descending
            ? clients.OrderByDescending(selector).ToList()
            : clients.OrderBy(selector).ToList();
    }

    private static ActivityDto ToActivityDto(ActivityLog log) =>
        new()
        {
            Id = log.Id,
            ClientId = log.ClientId,
            ActivityType = log.ActivityType,
            Description = log.Description,
            ActivityDateTime = log.ActivityDateTime,
            DelayReason = log.DelayReason,
            CreatedAt = log.CreatedAt
        };

    private static TaskDto ToTaskDto(TaskItem task) =>
        new()
        {
            Id = task.Id,
            ClientId = task.ClientId,
            Description = task.Description,
            DueDate = task.DueDate,
            Priority = task.Priority,
            AssignedTo = task.AssignedTo,
            IsDone = task.IsDone,
            DoneAt = task.DoneAt,
            AddedAt = task.AddedAt
        };
}
