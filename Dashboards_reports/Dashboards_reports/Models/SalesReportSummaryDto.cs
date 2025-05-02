namespace Dashboards_reports.Models
{
  public class SalesReportSummaryDto
  {
    public string? SalesMonth { get; set; }
    public string? Location { get; set; }
    public decimal InternalSales { get; set; }
    public decimal ExternalSales { get; set; }
    public decimal Target { get; set; }
  }
}
