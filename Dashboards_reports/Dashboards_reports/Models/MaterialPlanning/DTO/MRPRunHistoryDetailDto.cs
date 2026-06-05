namespace Dashboards_reports.Models.MaterialPlanning.DTO
{
    public class MRPRunHistoryDetailDto
    {
        public int WeekNo { get; set; }
        public decimal GrossRequirements { get; set; }
        public decimal ScheduledReceipts { get; set; }
        public decimal ProjectedAvailableBalance { get; set; }
        public decimal NetRequirements { get; set; }
        public decimal PlannedOrderReceipt { get; set; }
        public decimal PlannedOrderRelease { get; set; }
    }
}
