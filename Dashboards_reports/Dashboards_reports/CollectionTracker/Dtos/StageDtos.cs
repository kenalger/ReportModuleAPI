namespace Dashboards_reports.CollectionTracker.Dtos;

public sealed record StageDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed record CreateStageRequest
{
    public string Name { get; init; } = string.Empty;
}

public sealed record UpdateStageRequest
{
    public string Name { get; init; } = string.Empty;
}

public sealed record ReorderStagesRequest
{
    public List<int> StageIds { get; init; } = [];
}
