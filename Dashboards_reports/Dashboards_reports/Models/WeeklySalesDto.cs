namespace Dashboards_reports.Models
{
  public class WeeklySalesDto
  {
    public string? Location { get; set; }
    public decimal WeeklyTarget { get; set; }
    public int WeekNumber { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public decimal InternalSales { get; set; }
    public decimal ExternalSales { get; set; }
  }
}
