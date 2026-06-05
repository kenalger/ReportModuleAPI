namespace Dashboards_reports.CollectionTracker.Domain;

public sealed class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? UnitId { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public decimal? TotalContractPrice { get; set; }
    public string? ContactNumber { get; set; }
    public string? BrokerName { get; set; }
    public string FinancingType { get; set; } = "Bank";
    public string Stage { get; set; } = "Reservation";
    public DateTime? StageDate { get; set; }
    public DateTime? TargetDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public string DelayReason { get; set; } = "None";
    public string? SecondaryDelayReason { get; set; }
    public string? NextAction { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public string? Notes { get; set; }
    public DateTime AddedDate { get; set; }
    public string? ResolvedHow { get; set; }
    public string? ResolvedNotes { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<string> DelayReasons { get; } = [];
    public List<ActivityLog> Activities { get; } = [];
    public List<TaskItem> Tasks { get; } = [];
}
