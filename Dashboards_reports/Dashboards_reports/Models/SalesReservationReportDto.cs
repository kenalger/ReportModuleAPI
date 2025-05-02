namespace Dashboards_reports.Models
{
  public class SalesReservationReportDto
  {
    public string? SortMonth { get; set; }
    public string? DisplayMonth { get; set; }
    public string? Location { get; set; }
    public decimal Target { get; set; }
    public decimal InternalSales { get; set; }
    public decimal ExternalSales { get; set; }
  }
}
