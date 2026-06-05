using Dashboards_reports.Models.MaterialPlanning;
using Microsoft.EntityFrameworkCore;

namespace Dashboards_reports.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
       : base(options) { }

        public DbSet<MRPRun> MRPRuns { get; set; } = null!;
        public DbSet<MRPRunDetail> MRPRunDetails { get; set; } = null!;
        public DbSet<MaterialPlanningSetup> MaterialPlanningSetups { get; set; } = null!;
        public DbSet<InventoryItem> InventoryItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MaterialPlanningSetup>().HasNoKey();


            modelBuilder.Entity<MRPRunDetail>()
                    .HasIndex(x => new { x.MRPRunId, x.WeekNo })
                    .IsUnique();

            modelBuilder.Entity<MaterialPlanningSetup>()
        .HasKey(x => x.PlanningSetupId);

            modelBuilder.Entity<MaterialPlanningSetup>()
                .HasIndex(x => new { x.CompanyId, x.WarehouseId, x.MaterialId })
                .IsUnique();
        }
    }
}
