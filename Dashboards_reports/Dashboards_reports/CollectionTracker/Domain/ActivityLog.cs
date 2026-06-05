namespace Dashboards_reports.CollectionTracker.Domain;

public sealed class ActivityLog
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string ActivityType { get; set; } = "note";
    public string Description { get; set; } = string.Empty;
    public DateTime ActivityDateTime { get; set; }
    public string? CreatedBy { get; set; }
    public string? DelayReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
