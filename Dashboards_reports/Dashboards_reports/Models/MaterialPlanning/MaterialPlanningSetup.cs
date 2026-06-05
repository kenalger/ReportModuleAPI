using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboards_reports.Models.MaterialPlanning
{
    public class MaterialPlanningSetup
    {
        [Key]
        public int PlanningSetupId { get; set; }

        public int CompanyId { get; set; }
        public int WarehouseId { get; set; }
        public int MaterialId { get; set; }
        public decimal MinimumOrderQty { get; set; }

        [NotMapped]               // 👈 VERY IMPORTANT
        public decimal OnHandQty { get; set; }
        public decimal SafetyStock { get; set; }
        public int LeadTimeWeeks { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
