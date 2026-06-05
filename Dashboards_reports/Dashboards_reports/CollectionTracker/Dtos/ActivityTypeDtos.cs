namespace Dashboards_reports.CollectionTracker.Dtos;

public sealed record ActivityTypeDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed record CreateActivityTypeRequest
{
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}

public sealed record UpdateActivityTypeRequest
{
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}
