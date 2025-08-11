namespace Dashboards_reports.Models.KPI
{
  public class KpiPrToPoCycleTimeDto
  {
    public int ActualCycleTime { get; set; }
    public decimal? TargetValue { get; set; }
    public string? Target_type { get; set; }
    public string? Status { get; set; }
    public int ReportMonth { get; set; }
    public int ReportYear { get; set; }
  }

}
