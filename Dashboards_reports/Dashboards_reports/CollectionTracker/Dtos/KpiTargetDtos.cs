namespace Dashboards_reports.CollectionTracker.Dtos;

public sealed record KpiTargetDto
{
    public int StuckThresholdDays { get; init; }
    public decimal StuckRateTargetPercent { get; init; }
    public int LoanCycleTargetDays { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed record UpdateKpiTargetsRequest
{
    public int StuckThresholdDays { get; init; }
    public decimal StuckRateTargetPercent { get; init; }
    public int LoanCycleTargetDays { get; init; }
}
