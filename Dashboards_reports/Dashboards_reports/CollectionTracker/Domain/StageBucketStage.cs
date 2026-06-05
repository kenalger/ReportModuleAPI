namespace Dashboards_reports.CollectionTracker.Domain;

public sealed class StageBucketStage
{
    public int Id { get; set; }
    public int BucketId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
