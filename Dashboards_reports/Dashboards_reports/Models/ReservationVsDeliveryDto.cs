namespace Dashboards_reports.Models
{
  public class ReservationVsDeliveryDto
  {
    public string? SortMonth { get; set; }
    public string? DisplayMonth { get; set; }
    public string? Location { get; set; }
    public decimal ReservedAmount { get; set; }
    public decimal DeliveredAmount { get; set; }
    public decimal Variance { get; set; } 
  }
}
