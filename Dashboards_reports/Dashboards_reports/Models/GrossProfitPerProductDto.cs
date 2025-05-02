namespace Dashboards_reports.Models
{
  public class GrossProfitPerProductDto
  {
    public int ItemID { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? SortMonth { get; set; }       // Format: "yyyy-MM"
    public string? DisplayMonth { get; set; }    // Format: "MMM-yyyy" (e.g., Jan-2025)
    public decimal GrossProfit { get; set; }
    public decimal QuantityDelivered { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCOGS { get; set; }
  }
}
