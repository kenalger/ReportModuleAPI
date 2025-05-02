namespace Dashboards_reports.Models
{
  public class ARAgingDetailDto
  {
    public string? CustomerName { get; set; }
    public int InvoiceID { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public int DaysOverdue { get; set; }
    public string? AgingBucket { get; set; }
  }
}
