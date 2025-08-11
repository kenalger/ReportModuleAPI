namespace Dashboards_reports.Models.KPI
{
  public class POFulfillmentDetailDto
  {
    public int POID { get; set; }
    public decimal OrderedQty { get; set; }
    public decimal ReceivedQty { get; set; }
    public string? FulfillmentStatus { get; set; }  // "Fully Fulfilled" or "Not Fully Fulfilled"
  //  public int EmployeeId { get; set; }
    public string? PreparedBy { get; set; }
  }
}
