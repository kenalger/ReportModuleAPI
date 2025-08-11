namespace Dashboards_reports.Models.KPI
{
  public class POFulfillmentRateResponseDto
  {
    public List<POFulfillmentDetailDto>? Details { get; set; }
    public POFulfillmentSummaryDto? Summary { get; set; }
    public List<POFulfillmentTrendDto>? YTDTrend { get; set; }
    public List<POFulfillmentLeaderboardDto>? Leaderboard { get; set; } // Only populated if PreparedById is NULL

  }
}
