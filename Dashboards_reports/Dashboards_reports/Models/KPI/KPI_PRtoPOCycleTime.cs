namespace Dashboards_reports.Models.KPI
{
  public class KPI_PRtoPOCycleTime
  {
    public class KpiPrToPoCycleTimeDto
    {
      public int PRID { get; set; }
      public DateTime PRDate { get; set; }
      public int POID { get; set; }
      public DateTime PODate { get; set; }
      public string? POStatus { get; set; }
      public int CycleTime { get; set; }

      // KPI summary fields
      public double ActualCycleTime { get; set; }
      public double TargetValue { get; set; }
      public string? TargetType { get; set; }
      public string? Status { get; set; }
      public int ReportMonth { get; set; }
      public int ReportYear { get; set; }
    }

  }

}
