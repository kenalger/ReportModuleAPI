namespace Dashboards_reports.Models.KPI
{
  public class PrRrCycleTimeDetailDto
  {
    public int PRID { get; set; }
    public DateTime? PRDate { get; set; }
    public int? RRID { get; set; }
    public DateTime? RRDate { get; set; }
    public string? RRStatus { get; set; }
    public int CycleTime { get; set; }
  }

  public class PrRrCycleTimeSummaryDto
  {
    public double ActualCycleTime { get; set; }
    public double TargetValue { get; set; }
    public string? TargetType { get; set; }
    public string? Status { get; set; }
    public int ReportMonth { get; set; }
    public int ReportYear { get; set; }
  }

  public class KpiPrToRrCycleTimeTrend
  {
    public int Month { get; set; }
    public int Year { get; set; }
    public double CycleTime { get; set; }
  }

  public class PrRrCycleTimeResponseDto
  {
    public List<PrRrCycleTimeDetailDto>? Details { get; set; }
    public PrRrCycleTimeSummaryDto? Summary { get; set; }
    public List<KpiPrToRrCycleTimeTrend>? Trend { get; set; }
  }

}
