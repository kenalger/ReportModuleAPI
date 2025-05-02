using Dashboards_reports.Models;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace Dashboards_reports.Data

{
  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions<AppDbContext> options)
      : base(options) { }

    public DbSet<SalestMetricsDto> SalesMetrics { get; set; } = null!; // Needed for FromSqlRaw
    public DbSet<ARAgingSummaryDto> ARAgingSummaryDto { get; set; } = null!;
    public DbSet<ARAgingDetailDto> ARAgingDetailDto { get; set; } = null!;
    public DbSet<SalesReportSummaryDto> SalesReportSummaryDto { get; set; } = null!;
    public DbSet<TopSellingProductDetailDto> TopSellingProductDetailDtos { get; set; } = null!;
    public DbSet<SalesRecapDto> SalesRecapDto { get; set; } = null!;
    public DbSet<WeeklySalesDto> WeeklySalesDtos { get; set; } = null!;
    public DbSet<TopDeliveredItemDto> TopDeliveredItemDtos { get; set; } = null!;
    public DbSet<GrossProfitPerProductDto> GrossProfitPerProductDtos { get; set; } = null!;
    public DbSet<SalesReservationReportDto> SalesReservationReportDtos { get; set; } = null!;
    public DbSet<ReservationVsDeliveryDto> ReservationVsDeliveryDtos { get; set; } = null!;
    public DbSet<SalesTargetDto> SalesMonthlyTargets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<SalestMetricsDto>().HasNoKey();
      modelBuilder.Entity<ARAgingSummaryDto>().HasNoKey();
      modelBuilder.Entity<ARAgingDetailDto>().HasNoKey();
      modelBuilder.Entity<SalesReportSummaryDto>().HasNoKey();
      modelBuilder.Entity<SalesRecapDto>().HasNoKey();
      modelBuilder.Entity<WeeklySalesDto>().HasNoKey();
      modelBuilder.Entity<TopDeliveredItemDto>().HasNoKey();
      modelBuilder.Entity<GrossProfitPerProductDto>().HasNoKey();
      modelBuilder.Entity<SalesReservationReportDto>().HasNoKey();
      modelBuilder.Entity<TopSellingProductDetailDto>().HasNoKey();
      modelBuilder.Entity<ReservationVsDeliveryDto>().HasNoKey();

      modelBuilder.Entity<SalesTargetDto>(entity =>
      {
        entity.ToTable("SalesMonthlyTargets");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.TargetAmount)
              .HasColumnType("decimal(18,2)");

        entity.Property(e => e.CreatedBy);
      });

  }

  }
}
