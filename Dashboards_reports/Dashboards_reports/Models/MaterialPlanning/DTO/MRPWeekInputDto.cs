namespace Dashboards_reports.Models.MaterialPlanning.DTO
{
    public class MRPWeekInputDto
    {
        public int WeekNo { get; set; }
        public decimal GrossRequirements { get; set; }
        public decimal ScheduledReceipts { get; set; }
    }
}
