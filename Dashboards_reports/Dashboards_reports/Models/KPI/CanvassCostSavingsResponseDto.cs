namespace Dashboards_reports.Models.KPI
{
  public class CanvassCostSavingsResponseDto
  {
    public List<CanvassCostSavingsDetailDto> Details { get; set; } = new();
    public CanvassCostSavingsSummaryDto? Summary { get; set; }
    public List<CanvassCostSavingsTrendDto> Trend { get; set; } = new();
  }
}
