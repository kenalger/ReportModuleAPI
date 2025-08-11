namespace Dashboards_reports.Models.KPI
{
  public class PrPoCycleTimeResponseDto
  {
    public List<PrPoCycleTimeDetailDto>? Details { get; set; }
    public PrPoCycleTimeSummaryDto? Summary { get; set; }
    public List<KpiPrToPoCycleTimeTrend> Trend { get; set; } = new();
  }
}
