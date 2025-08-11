namespace Dashboards_reports.Models.KPI
{
  public class POFulfillmentSummaryDto
  {
    public decimal ActualFulfillmentRate { get; set; }
    public decimal TargetRate { get; set; }
    public string? TargetType { get; set; }   // e.g., "Percentage"
    public string? Status { get; set; }       // "Met" or "Below"
    public int ReportMonth { get; set; }
    public int ReportYear { get; set; }
  }
}
