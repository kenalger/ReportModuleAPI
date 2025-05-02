namespace Dashboards_reports.Models
{
  public class TopDeliveredItemDto
  {
    public int ItemId { get; set; }
    public string ItemName { get; set; }
    public string ItemDescription { get; set; }
    public decimal TotalDeliveredQty { get; set; }
  }
}
