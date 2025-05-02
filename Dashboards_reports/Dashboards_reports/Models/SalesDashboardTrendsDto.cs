namespace Dashboards_reports.Models
{
  public class SalesDashboardTrendsDto
  {
    public List<SalesTrendDto> SalesTrend { get; set; } = new();
    public List<ProfitTrendDto> ProfitTrend { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<SalesByLocationDto> SalesByLocation { get; set; } = new();
  }

  public class SalesTrendDto
  {
    public string Month { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
  }

  public class ProfitTrendDto
  {
    public string Month { get; set; } = string.Empty;
    public decimal GrossProfit { get; set; }
    public decimal DeliveryTotal { get; set; }
    public decimal SalesTotal { get; set; }

  }

  public class TopProductDto
  {
    public int ItemId { get; set; }
    public decimal TotalSales { get; set; }
  }

  public class SalesByLocationDto
  {
    public int LocationId { get; set; }
    public decimal TotalSales { get; set; }
  }
}
