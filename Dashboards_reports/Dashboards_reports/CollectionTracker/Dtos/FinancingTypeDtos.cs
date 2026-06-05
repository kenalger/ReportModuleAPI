namespace Dashboards_reports.CollectionTracker.Dtos;

public sealed record FinancingTypeDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed record CreateFinancingTypeRequest
{
    public string Name { get; init; } = string.Empty;
}

public sealed record UpdateFinancingTypeRequest
{
    public string Name { get; init; } = string.Empty;
}
