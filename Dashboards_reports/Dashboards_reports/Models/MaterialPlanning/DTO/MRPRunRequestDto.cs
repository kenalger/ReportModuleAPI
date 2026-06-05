namespace Dashboards_reports.Models.MaterialPlanning.DTO
{
    public class MRPRunRequestDto
    {
        public int CompanyId { get; set; }
        public int WarehouseId { get; set; }
        public int MaterialId { get; set; }

        public int StartWeek { get; set; }
        public int EndWeek { get; set; }

        public List<MRPWeekInputDto> Weeks { get; set; }
    }
}
