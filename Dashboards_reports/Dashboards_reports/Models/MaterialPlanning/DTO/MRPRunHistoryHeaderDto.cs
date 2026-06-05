namespace Dashboards_reports.Models.MaterialPlanning.DTO
{
    public class MRPRunHistoryHeaderDto
    {
        public int MRPRunId { get; set; }
        public int CompanyId { get; set; }
        public int WarehouseId { get; set; }
        public int MaterialId { get; set; }

        public int StartWeek { get; set; }
        public int EndWeek { get; set; }
        public DateTime RunDate { get; set; }
    }
}
