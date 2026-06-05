namespace Dashboards_reports.CollectionTracker.Dtos;

public sealed record DelayReasonDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed record CreateDelayReasonRequest
{
    public string Name { get; init; } = string.Empty;
}

public sealed record UpdateDelayReasonRequest
{
    public string Name { get; init; } = string.Empty;
}
