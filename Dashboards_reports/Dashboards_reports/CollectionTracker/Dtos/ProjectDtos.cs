namespace Dashboards_reports.CollectionTracker.Dtos;

public sealed record ProjectDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed record ProjectUnitDto
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal? TotalContractPrice { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed record UnitWithStatusDto
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal? TotalContractPrice { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public int? ClientId { get; init; }
    public string? ClientName { get; init; }
    public string? Stage { get; init; }
    public string Status { get; init; } = "available";
}

public sealed record CreateProjectRequest
{
    public string Name { get; init; } = string.Empty;
}

public sealed record UpdateProjectRequest
{
    public string Name { get; init; } = string.Empty;
}

public sealed record CreateProjectUnitRequest
{
    public string Name { get; init; } = string.Empty;
    public decimal? TotalContractPrice { get; init; }
}

public sealed record UpdateProjectUnitRequest
{
    public string Name { get; init; } = string.Empty;
    public decimal? TotalContractPrice { get; init; }
}
