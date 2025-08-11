namespace Dashboards_reports.Models.KPI
{
  public class KpiTargetDto
  {
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int ProjectId { get; set; }
    public string? KpiName { get; set; }
    public decimal TargetValue { get; set; }
    public string? TargetType { get; set; }
    public int TargetMonth { get; set; }
    public int TargetYear { get; set; }
    public string? Remarks { get; set; }
    public int? created_by { get; set; }
    public DateTime created_date { get; set; } = DateTime.UtcNow;
  }
}
