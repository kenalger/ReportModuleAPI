namespace Dashboards_reports.CollectionTracker.Dtos;

public sealed record StageBucketDto
{
    public int Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public List<string> AppliesTo { get; init; } = [];
    public List<string> Stages { get; init; } = [];
}

public sealed record CreateStageBucketRequest
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public List<string>? AppliesTo { get; init; }
}

public sealed record UpdateStageBucketRequest
{
    public string Name { get; init; } = string.Empty;
    public List<string>? AppliesTo { get; init; }
}

public sealed record SetBucketStagesRequest
{
    public List<string> Stages { get; init; } = [];
}

public sealed record ReorderBucketsRequest
{
    public List<int> BucketIds { get; init; } = [];
}
