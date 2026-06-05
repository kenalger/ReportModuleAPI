namespace Dashboards_reports.CollectionTracker.Domain;

public sealed class KpiTarget
{
    public int Id { get; set; }
    public int StuckThresholdDays { get; set; }
    public decimal StuckRateTargetPercent { get; set; }
    public int LoanCycleTargetDays { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
