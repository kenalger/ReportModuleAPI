namespace Dashboards_reports.Models.KPI
{
  public class PrPoCycleTimeDetailDto
  {
    public int PRID { get; set; }
    public DateTime PRDate { get; set; }
    public int POID { get; set; }
    public DateTime PODate { get; set; }
    public string? POStatus { get; set; }
    public int CycleTime { get; set; }
  }
}
