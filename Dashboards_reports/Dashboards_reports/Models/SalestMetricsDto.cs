using Microsoft.EntityFrameworkCore;

namespace Dashboards_reports.Models
{
  [Keyless]
  public class SalestMetricsDto
  {
    public decimal totalSalesInvoice { get; set; }
    public decimal totalCollection { get; set; }
    public decimal outstandingReceivables { get; set; }
    public int salesInvoices { get; set; }
    public int collectionReceipts { get; set; }
  }
}
