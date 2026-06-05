namespace Dashboards_reports.Models.MaterialPlanning
{
    public class MRPRun
    {
        public int MRPRunId { get; set; }

        public int CompanyId { get; set; }
        public int WarehouseId { get; set; }
        public int MaterialId { get; set; }

        public int StartWeek { get; set; }
        public int EndWeek { get; set; }
        public DateTime RunDate { get; set; }

       // public string Status { get; set; } = "DRAFT";
        // DRAFT | CONFIRMED | CLOSED

        public ICollection<MRPRunDetail> Details { get; set; }
    }
}
