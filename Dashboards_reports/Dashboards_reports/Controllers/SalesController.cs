using Dashboards_reports.Data;
using Dashboards_reports.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Dashboards_reports.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class SalesController : ControllerBase
  {
    //public IActionResult Index()
    //{
    //  return View();
    //}

    private readonly AppDbContext _context;
    public SalesController(AppDbContext context)
    {
      _context = context;
    }

    [HttpPost("sales-target")]
    public async Task<IActionResult> SaveTargets([FromBody] List<SalesTargetDto> targets)
    {
      foreach (var item in targets)
      {
        var existing = await _context.SalesMonthlyTargets
            .FirstOrDefaultAsync(x => x.CompanyId == item.CompanyId &&
                                      x.ProjectId == item.ProjectId &&
                                      x.TargetYear == item.TargetYear &&
                                      x.TargetMonth == item.TargetMonth);

        if (existing != null)
        {
          existing.TargetAmount = item.TargetAmount;
        }
        else
        {
          _context.SalesMonthlyTargets.Add(new SalesTargetDto
          {
            CompanyId = item.CompanyId,
            ProjectId = item.ProjectId,
            TargetYear = item.TargetYear,
            TargetMonth = item.TargetMonth,
            TargetAmount = item.TargetAmount,
            CreatedBy = item.CreatedBy
          });
        }
      }

      await _context.SaveChangesAsync();
      return Ok();
    }
    [HttpGet("sales-target")]
    public async Task<IActionResult> GetSalesTarget(int companyId, int projectId, int year)
    {
      var targets = await _context.SalesMonthlyTargets
          .Where(t => t.CompanyId == companyId && t.ProjectId == projectId && t.TargetYear == year)
          .Select(t => new {
            t.TargetMonth,
            t.TargetAmount
          })
          .ToListAsync();

      return Ok(targets);
    }
    [HttpGet ("agingSummary")]
    public async Task<ActionResult<IEnumerable<ARAgingSummaryDto>>> GetARAgingSummary()
    {
      var result = new List<ARAgingSummaryDto>();

      using var connection = _context.Database.GetDbConnection();
      await connection.OpenAsync();

      using var command = connection.CreateCommand();
      command.CommandText = "sp_AR_Aging_Summary";
      command.CommandType = CommandType.StoredProcedure;

      using var reader = await command.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        result.Add(new ARAgingSummaryDto
        {
          CustomerId = Convert.ToInt32(reader["id"]),
          CustomerName = reader["fullname"].ToString() ?? "",
          Days0To30 = Convert.ToDecimal(reader["0_30_Days"]),
          Days31To60 = Convert.ToDecimal(reader["31_60_Days"]),
          Days61To90 = Convert.ToDecimal(reader["61_90_Days"]),
          Over90Days = Convert.ToDecimal(reader["Over_90_Days"]),
          TotalOutstanding = Convert.ToDecimal(reader["TotalOutstanding"])
        });
      }

      return Ok(result);
    }

    [HttpGet("agingDetail")]
    public async Task<ActionResult<IEnumerable<ARAgingDetailDto>>> GetARAgingDetail()
    {

      var results = await _context.ARAgingDetailDto
          .FromSqlRaw("EXEC sp_AR_Aging_Detail")
          .ToListAsync();

      return Ok(results);
    }

    [HttpGet("gross-profit-per-item-report")]
    public async Task<ActionResult<IEnumerable<GrossProfitPerProductDto>>> GetGrossProfitPerProduct(int companyId, DateTime startDate, DateTime endDate )
    {
      var results = await _context.GrossProfitPerProductDtos
        .FromSqlRaw(
            "EXEC sp_GrossProfitPerProduct @companyId, @startDate, @endDate",
              new SqlParameter("@companyId", companyId),
              new SqlParameter("@startDate", startDate),
              new SqlParameter("@endDate", endDate)
        )
        .ToListAsync();

        return Ok(results);
    }

    [HttpGet("sales-reservation-report")]
    public async Task<IActionResult> GetSalesReservationReport(DateTime startDate, DateTime endDate, int companyId)
    {
      var result = await _context.SalesReservationReportDtos
          .FromSqlRaw("EXEC dbo.sp_GetSalesReservationReportByMonthAndLocation @StartDate = {0}, @EndDate = {1}, @CompanyId = {2}",
                      startDate, endDate, companyId)
          .ToListAsync();

      return Ok(result);
    }

    [HttpGet("reservation-delivery-comparison")]
    public async Task<IActionResult> GetReservationVsDelivery(DateTime startDate, DateTime endDate, int companyId)
    {
      var result = await _context.ReservationVsDeliveryDtos
          .FromSqlRaw("EXEC dbo.sp_ReservationVsDeliveryByMonthAndLocation  @StartDate = {0}, @EndDate = {1}, @CompanyId = {2}",
                      startDate, endDate, companyId)
          .ToListAsync();

      return Ok(result);
    }

    [HttpGet("top-delivered-items")]
    public async Task<ActionResult<IEnumerable<TopDeliveredItemDto>>> GetTopDeliveredItems()
    {
      var results = await _context.TopDeliveredItemDtos
        .FromSqlRaw(
          "EXEC sp_GetTopDeliveredItems"
        )
        .ToListAsync();

      return Ok(results);
    }

    [HttpGet("sales-recap-summary")]
    public async Task<ActionResult<IEnumerable<SalesRecapDto>>> GetSalesRecap(DateTime endDate, int companyId)
    {
      
      var results = await _context.SalesRecapDto
        .FromSqlRaw(
          "EXEC sp_SalesRecapDashboard @endDate, @companyId",
              new SqlParameter("@endDate", endDate),
              new SqlParameter("@companyId", companyId)
        )
        .ToListAsync();

      return Ok(results);
    }

    [HttpGet("top-selling-products")]
    public async Task<ActionResult<IEnumerable<TopSellingProductDetailDto>>> GetTopSellingProductDetails(DateTime startDate, DateTime endDate, int companyId)
    {
      var results = await _context.TopSellingProductDetailDtos
        .FromSqlRaw(
          "EXEC sp_TopSellingProductDetails @startDate, @endDate, @companyId",
              new SqlParameter("@startDate", startDate),
              new SqlParameter("@endDate", endDate),
              new SqlParameter("@companyId", companyId)
        )
        .ToListAsync();

      return Ok(results);
    }

    [HttpGet("weekly-sales")]
    public async Task<ActionResult<IEnumerable<WeeklySalesDto>>> GetWeeklySalesReport( DateTime endDate, int companyId)
    {
      var results = await _context.WeeklySalesDtos
        .FromSqlRaw(
          "EXEC sp_GetWeeklySalesByLocation @endDate, @companyId",
             
              new SqlParameter("@endDate", endDate),
              new SqlParameter("@companyId", companyId)
        )
        .ToListAsync();

      return Ok(results);
    }

    [HttpGet("sales-report-summary")]
    public async Task<ActionResult<IEnumerable<SalesReportSummaryDto>>> GetSalesReportSummary(DateTime startDate, DateTime endDate, int companyId)
    {
      var results = await _context.SalesReportSummaryDto
        .FromSqlRaw (
          "EXEC sp_GetSalesReportByMonthAndLocation @startDate, @endDate, @companyId",
              new SqlParameter("@startDate", startDate),
              new SqlParameter("@endDate", endDate),
              new SqlParameter("@companyId", companyId)
        )
        .ToListAsync ();

      return Ok(results);
    }

    [HttpGet("dashboard/Sales")]
    public async Task<SalestMetricsDto?> GetKeyMetricsAsync(DateTime startDate, DateTime endDate, int companyId = 5)
    {
      var result = await _context.SalesMetrics
          .FromSqlRaw(
              "EXEC sp_GetKeySalesMetrics @startDate, @endDate, @companyId",
              new SqlParameter("@startDate", startDate),
              new SqlParameter("@endDate", endDate),
              new SqlParameter("@companyId", companyId)
          )
          .ToListAsync();
      return result.FirstOrDefault() ?? throw new InvalidOperationException("No sales metrics found.");
    }

    [HttpGet("dashboard/sales-trends")]
    public async Task<SalesDashboardTrendsDto> GetSalesDashboardTrendsAsync(DateTime startDate, DateTime endDate, int companyId = 5)
    {
      var result = new SalesDashboardTrendsDto();

      using var connection = _context.Database.GetDbConnection();
      await connection.OpenAsync();

      using var command = connection.CreateCommand();
      command.CommandText = "sp_GetSalesDashboardTrends";
      command.CommandType = CommandType.StoredProcedure;

      command.Parameters.Add(new SqlParameter("@startdate", startDate));
      command.Parameters.Add(new SqlParameter("@enddate", endDate));
      command.Parameters.Add(new SqlParameter("@companyid", companyId));

      using var reader = await command.ExecuteReaderAsync();

      // Sales Trend
      while (await reader.ReadAsync())
      {
        result.SalesTrend.Add(new SalesTrendDto
        {
          Month = reader["month"].ToString() ?? "",
          TotalSales = Convert.ToDecimal(reader["totalsales"])
        });
      }

      await reader.NextResultAsync();

      // Profit Trend
      while (await reader.ReadAsync())
      {
        result.ProfitTrend.Add(new ProfitTrendDto
        {
          Month = reader["month"].ToString() ?? "",
          GrossProfit = Convert.ToDecimal(reader["grossprofit"]),
          DeliveryTotal = Convert.ToDecimal(reader["deliverytotal"]),
          SalesTotal = Convert.ToDecimal(reader["salestotal"])
        });
      }

      await reader.NextResultAsync();

      // Top Products
      while (await reader.ReadAsync())
      {
        result.TopProducts.Add(new TopProductDto
        {
          ItemId = Convert.ToInt32(reader["itemid"]),
          TotalSales = Convert.ToDecimal(reader["totalsales"])
        });
      }

      await reader.NextResultAsync();

      // Sales by Location
      while (await reader.ReadAsync())
      {
        result.SalesByLocation.Add(new SalesByLocationDto
        {
          LocationId = Convert.ToInt32(reader["locationid"]),
          TotalSales = Convert.ToDecimal(reader["totalsales"])
        });
      }

      return result;
    }


  }
}
