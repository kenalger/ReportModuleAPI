namespace Dashboards_reports.Models
{
  public class SalesRecapDto
  {
    public decimal TargetSales { get; set; }
    public decimal ActualSales { get; set; }
    public decimal UnderNegotiationAmount { get; set; }
    public decimal LostSalesAmount { get; set; }
    public decimal OpportunityToWinPercentage { get; set; }
    public decimal TargetToWinPercentage { get; set; }


  }
}
