namespace Dashboards_reports.CollectionTracker.Dtos;

public sealed record ClientQueryParams(
    string? Filter = "all",
    string? Search = null,
    string? SortField = null,
    string? SortDir = "asc",
    int? ProjectId = null);

public sealed record UpsertClientRequest
{
    public string Name { get; init; } = string.Empty;
    public int? ProjectId { get; init; }
    public int? UnitId { get; init; }
    public string Unit { get; init; } = string.Empty;
    public decimal? TotalContractPrice { get; init; }
    public string? ContactNumber { get; init; }
    public string? BrokerName { get; init; }
    public string FinancingType { get; init; } = "Bank";
    public string Stage { get; init; } = "Reservation";
    public DateTime? StageDate { get; init; }
    public DateTime? TargetDate { get; init; }
    public DateTime? ResolvedDate { get; init; }
    public IReadOnlyList<string>? DelayReasons { get; init; }
    public string DelayReason { get; init; } = "None";
    public string? SecondaryDelayReason { get; init; }
    public string? NextAction { get; init; }
    public DateTime? FollowUpDate { get; init; }
    public string? Notes { get; init; }
    public string? CreatedBy { get; init; }
    public string? ModifiedBy { get; init; }
}

public sealed record ResolveClientRequest
{
    public string ResolvedHow { get; init; } = string.Empty;
    public DateTime? ResolvedDate { get; init; }
    public string? ResolvedNotes { get; init; }
}

public sealed record AddActivityRequest
{
    public string ActivityType { get; init; } = "note";
    public string Description { get; init; } = string.Empty;
    public DateTime? ActivityDateTime { get; init; }
    public string? DelayReason { get; init; }
}

public sealed record AddTaskRequest
{
    public string Description { get; init; } = string.Empty;
    public DateTime? DueDate { get; init; }
    public string Priority { get; init; } = "medium";
    public string? AssignedTo { get; init; }
}

public sealed record UpdateTaskStatusRequest
{
    public bool IsDone { get; init; }
}

public record ClientListItemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int? ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public int? UnitId { get; init; }
    public string Unit { get; init; } = string.Empty;
    public decimal? TotalContractPrice { get; init; }
    public string? ContactNumber { get; init; }
    public string? BrokerName { get; init; }
    public string FinancingType { get; init; } = string.Empty;
    public string Stage { get; init; } = string.Empty;
    public DateTime? StageDate { get; init; }
    public DateTime? TargetDate { get; init; }
    public DateTime? ResolvedDate { get; init; }
    public IReadOnlyList<string> DelayReasons { get; init; } = [];
    public string DelayReason { get; init; } = "None";
    public string? SecondaryDelayReason { get; init; }
    public string? NextAction { get; init; }
    public DateTime? FollowUpDate { get; init; }
    public string? Notes { get; init; }
    public DateTime AddedDate { get; init; }
    public string? CreatedBy { get; init; }
    public string? ModifiedBy { get; init; }
    public string? ResolvedHow { get; init; }
    public string? ResolvedNotes { get; init; }
    public int DaysInStage { get; init; }
    public int ProgressPercent { get; init; }
    public string AgingStatus { get; init; } = "ok";
    public bool IsResolved { get; init; }
    public bool IsCancelled { get; init; }
    public bool IsTargetOverdue { get; init; }
    public int ActivityCount { get; init; }
    public int PendingTaskCount { get; init; }
    public int OverdueTaskCount { get; init; }
}

public sealed record ActivityDto
{
    public int Id { get; init; }
    public int ClientId { get; init; }
    public string ActivityType { get; init; } = "note";
    public string Description { get; init; } = string.Empty;
    public DateTime ActivityDateTime { get; init; }
    public string? DelayReason { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record TaskDto
{
    public int Id { get; init; }
    public int ClientId { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateTime? DueDate { get; init; }
    public string Priority { get; init; } = "medium";
    public string? AssignedTo { get; init; }
    public bool IsDone { get; init; }
    public DateTime? DoneAt { get; init; }
    public DateTime AddedAt { get; init; }
}

public sealed record ClientDetailDto : ClientListItemDto
{
    public int TotalDays { get; init; }
    public IReadOnlyList<ActivityDto> Activities { get; init; } = [];
    public IReadOnlyList<TaskDto> Tasks { get; init; } = [];
}

public sealed record InsightsEmailRequest
{
    public IReadOnlyList<string> Recipients { get; init; } = [];
    public string Subject { get; init; } = string.Empty;
    public string BodyHtml { get; init; } = string.Empty;
    public string? CompanyName { get; init; }
    public string? GeneratedAt { get; init; }
}

public sealed record RecentActivityDto
{
    public int Id { get; init; }
    public int ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string? ProjectName { get; init; }
    public string ActivityType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime ActivityDateTime { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; }
}
