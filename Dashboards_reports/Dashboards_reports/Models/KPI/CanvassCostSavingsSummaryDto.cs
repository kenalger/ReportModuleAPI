namespace Dashboards_reports.Models.KPI
{
  public class CanvassCostSavingsSummaryDto
  {
    public int TotalItems { get; set; }
    public decimal ActualSavingsPercent { get; set; }
    public decimal TargetSavingsPercent { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ReportMonth { get; set; }
    public int ReportYear { get; set; }
  }
}
