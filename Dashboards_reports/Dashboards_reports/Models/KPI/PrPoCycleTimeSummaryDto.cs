namespace Dashboards_reports.Models.KPI
{
  public class PrPoCycleTimeSummaryDto
  {
    public double ActualCycleTime { get; set; }
    public double TargetValue { get; set; }
    public string? TargetType { get; set; }
    public string? Status { get; set; }
    public int ReportMonth { get; set; }
    public int ReportYear { get; set; }
  }
}
