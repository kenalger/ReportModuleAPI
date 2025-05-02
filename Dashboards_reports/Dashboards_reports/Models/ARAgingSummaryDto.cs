namespace Dashboards_reports.Models
{
  public class ARAgingSummaryDto
  {
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal Days0To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90Days { get; set; }
    public decimal TotalOutstanding { get; set; }
  }
}
