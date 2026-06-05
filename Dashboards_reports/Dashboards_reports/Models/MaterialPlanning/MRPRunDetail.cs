namespace Dashboards_reports.Models.MaterialPlanning
{
    public class MRPRunDetail
    {
        public int MRPRunDetailId { get; set; }
        public int MRPRunId { get; set; }
        public MRPRun MRPRun { get; set; }
        public int WeekNo { get; set; }

        public decimal GrossRequirements { get; set; }
        public decimal ScheduledReceipts { get; set; }
        public decimal ProjectedAvailableBalance { get; set; }
        public decimal NetRequirements { get; set; }
        public decimal PlannedOrderReceipt { get; set; }
        public decimal PlannedOrderRelease { get; set; }

        
    }
}
