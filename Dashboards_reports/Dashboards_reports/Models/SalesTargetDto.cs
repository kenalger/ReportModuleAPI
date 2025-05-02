namespace Dashboards_reports.Models
{
  public class SalesTargetDto
  {
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int ProjectId { get; set; }
    public int TargetYear { get; set; }
    public int TargetMonth { get; set; }
    public decimal TargetAmount { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
  }
}
