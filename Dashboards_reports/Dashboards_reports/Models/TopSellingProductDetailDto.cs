namespace Dashboards_reports.Models
{
  public class TopSellingProductDetailDto
  {
    public int ItemID { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? ItemDescription { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    public decimal QuantityDelivered { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
  }
}
