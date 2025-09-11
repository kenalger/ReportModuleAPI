using Dashboards_reports.Data;
using Dashboards_reports.Models;
using Dashboards_reports.Models.KPI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Dashboards_reports.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class KpiPurchasingController : ControllerBase
  {
    private readonly AppDbContext _context;
    public KpiPurchasingController(AppDbContext context)
    {
      _context = context;
    }

    [HttpGet("pr-to-po-cycle-time")]
    public async Task<ActionResult<PrPoCycleTimeResponseDto>> GetPrToPoCycleTime(int Month, int Year, int CompanyId)
    {
      var detailList = new List<PrPoCycleTimeDetailDto>();
      PrPoCycleTimeSummaryDto? summary = null;
      var trendList = new List<KpiPrToPoCycleTimeTrend>();

      var connection = _context.Database.GetDbConnection();
      await using (connection)
      {
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "dbPRCenter.dbo.sp_KPI_PRtoPOCycleTime";
        command.CommandType = CommandType.StoredProcedure;

        // Add parameters
        var monthParam = command.CreateParameter();
        monthParam.ParameterName = "@Month";
        monthParam.Value = Month;
        command.Parameters.Add(monthParam);

        var yearParam = command.CreateParameter();
        yearParam.ParameterName = "@Year";
        yearParam.Value = Year;
        command.Parameters.Add(yearParam);

        var companyParam = command.CreateParameter();
        companyParam.ParameterName = "@CompanyId";
        companyParam.Value = CompanyId;
        command.Parameters.Add(companyParam);

        await using var reader = await command.ExecuteReaderAsync();

        //  Result Set 1: PR → PO details
        while (await reader.ReadAsync())
        {
          detailList.Add(new PrPoCycleTimeDetailDto
          {
            //PRID = reader.GetInt32(0),
            //PRDate = reader.GetDateTime(1),
            //POID = reader.GetInt32(2),
            //PODate = reader.GetDateTime(3),
            //POStatus = reader.IsDBNull(4) ? null : reader.GetString(4),
            //CycleTime = reader.GetInt32(5)
            PRID = reader.GetInt32(0),
            PRDate = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1),
            POID = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
            PODate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
            POStatus = reader.IsDBNull(4) ? null : reader.GetString(4),
            CycleTime = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
          });
        }

       

        //  Result Set 2: KPI Summary
        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
          summary = new PrPoCycleTimeSummaryDto
          {
            ActualCycleTime = reader.IsDBNull(0) ? 0 : reader.GetDouble(0),
            TargetValue = reader.IsDBNull(1) ? 0 : reader.GetDouble(1),
            TargetType = reader.IsDBNull(2) ? null : reader.GetString(2),
            Status = reader.IsDBNull(3) ? null : reader.GetString(3),
            ReportMonth = reader.GetInt32(4),
            ReportYear = reader.GetInt32(5)
          };
        }

        //  Result Set 3: Trend Data
        if (await reader.NextResultAsync())
        {
          while (await reader.ReadAsync())
          {
            trendList.Add(new KpiPrToPoCycleTimeTrend
            {
              Month = reader.GetInt32(0),
              Year = reader.GetInt32(1),
              CycleTime = reader.IsDBNull(2) ? 0 : Convert.ToDouble(reader.GetDecimal(2))
            });
          }
        }
      }

      // Combine everything into the response DTO
      var response = new PrPoCycleTimeResponseDto
      {
        Details = detailList,
        Summary = summary,
        Trend = trendList
      };

      return Ok(response);
    }

    [HttpGet("PO-Fulfillment-Rate")]
    public async Task<ActionResult<POFulfillmentRateResponseDto>> GetPoFulfillmentRate(
     int Month,
     int Year,
     int CompanyId,
     int? PreparedById = null)
    {
      var detailList = new List<POFulfillmentDetailDto>();
      POFulfillmentSummaryDto? summary = null;
      var trendList = new List<POFulfillmentTrendDto>();
      var leaderboardList = new List<POFulfillmentLeaderboardDto>();

      var connection = _context.Database.GetDbConnection();
      await using (connection)
      {
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "dbPRCenter.dbo.sp_KPI_POFulfillmentRate";
        command.CommandType = CommandType.StoredProcedure;

        // Add parameters
        var monthParam = command.CreateParameter();
        monthParam.ParameterName = "@Month";
        monthParam.Value = Month;
        command.Parameters.Add(monthParam);

        var yearParam = command.CreateParameter();
        yearParam.ParameterName = "@Year";
        yearParam.Value = Year;
        command.Parameters.Add(yearParam);

        var companyParam = command.CreateParameter();
        companyParam.ParameterName = "@CompanyId";
        companyParam.Value = CompanyId;
        command.Parameters.Add(companyParam);

        var preparedByParam = command.CreateParameter();
        preparedByParam.ParameterName = "@PreparedById";
        preparedByParam.Value = (object?)PreparedById ?? DBNull.Value;
        command.Parameters.Add(preparedByParam);

        await using var reader = await command.ExecuteReaderAsync();

        // ========================================
        // Result Set 1: Details (Current Month)
        // ========================================
        while (await reader.ReadAsync())
        {
          detailList.Add(new POFulfillmentDetailDto
          {
            POID = reader.GetInt32(0),
            OrderedQty = reader.GetDecimal(1),
            ReceivedQty = reader.GetDecimal(2),
            FulfillmentStatus = reader.GetString(3),
           // EmployeeId = reader.GetInt32(4),
            PreparedBy = reader.GetString(4)
          });
        }

        // ========================================
        // Result Set 2: KPI Summary
        // ========================================
        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
          summary = new POFulfillmentSummaryDto
          {
            ActualFulfillmentRate = reader.GetDecimal(0),
            TargetRate = reader.GetDecimal(1),
            TargetType = reader.GetString(2),
            Status = reader.GetString(3),
            ReportMonth = reader.GetInt32(4),
            ReportYear = reader.GetInt32(5)
          };
        }

        // ========================================
        // Result Set 3: YTD Trend
        // ========================================
        if (await reader.NextResultAsync())
        {
          while (await reader.ReadAsync())
          {
            trendList.Add(new POFulfillmentTrendDto
            {
              Month = reader.GetInt32(0),
              Year = reader.GetInt32(1),
              FulfillmentRate = reader.GetDecimal(2)
            });
          }
        }

        // ========================================
        // Result Set 4: PreparedBy Leaderboard (Only if PreparedById is NULL)
        // ========================================
        if (await reader.NextResultAsync())
        {
          while (await reader.ReadAsync())
          {
            leaderboardList.Add(new POFulfillmentLeaderboardDto
            {
              PreparedBy = reader.GetString(0),
              YTDFulfillmentRate = reader.GetDecimal(1)
            });
          }
        }
      }

      // Combine everything into the response DTO
      var response = new POFulfillmentRateResponseDto
      {
        Details = detailList,
        Summary = summary,
        YTDTrend = trendList,
        Leaderboard = leaderboardList
      };

      return Ok(response);
    }

    [HttpGet("Canvass-Cost-Savings")]
    public async Task<ActionResult<CanvassCostSavingsResponseDto>> GetCanvassCostSavings(
    int Month,
    int Year,
    int CompanyId)
    {
      var detailList = new List<CanvassCostSavingsDetailDto>();
      CanvassCostSavingsSummaryDto? summary = null;
      var trendList = new List<CanvassCostSavingsTrendDto>();

      var connection = _context.Database.GetDbConnection();
      await using (connection)
      {
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "dbPRCenter.dbo.sp_KPI_CanvassCostSavings";
        command.CommandType = CommandType.StoredProcedure;

        // Add parameters
        var monthParam = command.CreateParameter();
        monthParam.ParameterName = "@Month";
        monthParam.Value = Month;
        command.Parameters.Add(monthParam);

        var yearParam = command.CreateParameter();
        yearParam.ParameterName = "@Year";
        yearParam.Value = Year;
        command.Parameters.Add(yearParam);

        var companyParam = command.CreateParameter();
        companyParam.ParameterName = "@CompanyId";
        companyParam.Value = CompanyId;
        command.Parameters.Add(companyParam);

        await using var reader = await command.ExecuteReaderAsync();

        // ========================================
        // Result Set 1: Details (Item-level savings)
        // ========================================
        while (await reader.ReadAsync())
        {
          detailList.Add(new CanvassCostSavingsDetailDto
          {
            PrDetailsId = reader.GetInt32(0),
            AvgAmount = reader.GetDecimal(1),
            SelectedAmount = reader.GetDecimal(2),
            SavingsPercent = reader.GetDecimal(3)
          });
        }

        // ========================================
        // Result Set 2: KPI Summary
        // ========================================
        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
          summary = new CanvassCostSavingsSummaryDto
          {
            TotalItems = reader.GetInt32(0),
            ActualSavingsPercent = reader.GetDecimal(1),
            TargetSavingsPercent = reader.GetDecimal(2),
            TargetType = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            Status = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            ReportMonth = reader.GetInt32(5),
            ReportYear = reader.GetInt32(6)
          };
        }

        // ========================================
        // Result Set 3: Trend (YTD)
        // ========================================
        if (await reader.NextResultAsync())
        {
          while (await reader.ReadAsync())
          {
            trendList.Add(new CanvassCostSavingsTrendDto
            {
              Month = reader.GetInt32(0),
              Year = reader.GetInt32(1),
              SavingsPercent = reader.GetDecimal(2)
            });
          }
        }
      }

      // Combine everything into the response DTO
      var response = new CanvassCostSavingsResponseDto
      {
        Details = detailList,
        Summary = summary,
        Trend = trendList
      };

      return Ok(response);
    }

    [HttpGet("pr-to-rr-cycle-time")]
    public async Task<ActionResult<PrRrCycleTimeResponseDto>> GetPrToRrCycleTime(int Month, int Year, int CompanyId)
    {
      var detailList = new List<PrRrCycleTimeDetailDto>();
      PrRrCycleTimeSummaryDto? summary = null;
      var trendList = new List<KpiPrToRrCycleTimeTrend>();

      var connection = _context.Database.GetDbConnection();
      await using (connection)
      {
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "dbPRCenter.dbo.sp_KPI_PRtoRRCycleTime"; // 👈 new SP
        command.CommandType = CommandType.StoredProcedure;

        // Add parameters
        var monthParam = command.CreateParameter();
        monthParam.ParameterName = "@Month";
        monthParam.Value = Month;
        command.Parameters.Add(monthParam);

        var yearParam = command.CreateParameter();
        yearParam.ParameterName = "@Year";
        yearParam.Value = Year;
        command.Parameters.Add(yearParam);

        var companyParam = command.CreateParameter();
        companyParam.ParameterName = "@CompanyId";
        companyParam.Value = CompanyId;
        command.Parameters.Add(companyParam);

        await using var reader = await command.ExecuteReaderAsync();

        //  Result Set 1: PR → RR details
        while (await reader.ReadAsync())
        {
          detailList.Add(new PrRrCycleTimeDetailDto
          {
            PRID = reader.GetInt32(0),
            PRDate = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1),
            RRID = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
            RRDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
            RRStatus = reader.IsDBNull(4) ? null : reader.GetString(4),
            CycleTime = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
          });
        }

        //  Result Set 2: KPI Summary
        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
          summary = new PrRrCycleTimeSummaryDto
          {
            ActualCycleTime = reader.IsDBNull(0) ? 0 : reader.GetDouble(0),
            TargetValue = reader.IsDBNull(1) ? 0 : reader.GetDouble(1),
            TargetType = reader.IsDBNull(2) ? null : reader.GetString(2),
            Status = reader.IsDBNull(3) ? null : reader.GetString(3),
            ReportMonth = reader.GetInt32(4),
            ReportYear = reader.GetInt32(5)
          };
        }

        //  Result Set 3: Trend Data
        if (await reader.NextResultAsync())
        {
          while (await reader.ReadAsync())
          {
            trendList.Add(new KpiPrToRrCycleTimeTrend
            {
              Month = reader.GetInt32(0),
              Year = reader.GetInt32(1),
              CycleTime = reader.IsDBNull(2) ? 0 : Convert.ToDouble(reader.GetDecimal(2))
            });
          }
        }
      }

      // Combine everything into the response DTO
      var response = new PrRrCycleTimeResponseDto
      {
        Details = detailList,
        Summary = summary,
        Trend = trendList
      };

      return Ok(response);
    }


    [HttpPost("saveKpiTargets")]
    public async Task<IActionResult> SaveKpiTargets([FromBody] List<KpiTargetDto> targets)
    {
      using var connection = _context.Database.GetDbConnection();
      await connection.OpenAsync();

      foreach (var dto in targets)
      {
        using var command = connection.CreateCommand();
        command.CommandText = "dbPRCenter.dbo.SaveProcurementKpiTarget";
        command.CommandType = CommandType.StoredProcedure;

        var idParam = command.CreateParameter();
        idParam.ParameterName = "@Id";
        idParam.Value = dto.Id;
        idParam.DbType = DbType.Int32;
        command.Parameters.Add(idParam);

        var companyParam = command.CreateParameter();
        companyParam.ParameterName = "@CompanyId";
        companyParam.Value = dto.CompanyId;
        companyParam.DbType = DbType.Int32;
        command.Parameters.Add(companyParam);

        var projectParam = command.CreateParameter();
        projectParam.ParameterName = "@ProjectId";
        projectParam.Value = dto.ProjectId;
        projectParam.DbType = DbType.Int32;
        command.Parameters.Add(projectParam);

        var kpiNameParam = command.CreateParameter();
        kpiNameParam.ParameterName = "@KpiName";
        kpiNameParam.Value = dto.KpiName ?? string.Empty;
        kpiNameParam.DbType = DbType.String;
        command.Parameters.Add(kpiNameParam);

        var valueParam = command.CreateParameter();
        valueParam.ParameterName = "@TargetValue";
        valueParam.Value = dto.TargetValue;
        valueParam.DbType = DbType.Decimal;
        command.Parameters.Add(valueParam);

        var typeParam = command.CreateParameter();
        typeParam.ParameterName = "@TargetType";
        typeParam.Value = dto.TargetType ?? string.Empty;
        typeParam.DbType = DbType.String;
        command.Parameters.Add(typeParam);

        var monthParam = command.CreateParameter();
        monthParam.ParameterName = "@TargetMonth";
        monthParam.Value = dto.TargetMonth;
        monthParam.DbType = DbType.Int32;
        command.Parameters.Add(monthParam);

        var yearParam = command.CreateParameter();
        yearParam.ParameterName = "@TargetYear";
        yearParam.Value = dto.TargetYear;
        yearParam.DbType = DbType.Int32;
        command.Parameters.Add(yearParam);

        var remarksParam = command.CreateParameter();
        remarksParam.ParameterName = "@Remarks";
        remarksParam.Value = dto.Remarks ?? string.Empty;
        remarksParam.DbType = DbType.String;
        command.Parameters.Add(remarksParam);

        var createdByParam = command.CreateParameter();
        createdByParam.ParameterName = "@Created_By";
        createdByParam.Value = dto.created_by;
        createdByParam.DbType = DbType.Int16;
        command.Parameters.Add(createdByParam);

        var createdDateParam = command.CreateParameter();
        createdDateParam.ParameterName = "@Created_Date";
        createdDateParam.Value = dto.created_date;
        createdDateParam.DbType = DbType.DateTime;
        command.Parameters.Add(createdDateParam);

        await command.ExecuteNonQueryAsync();
      }

      return Ok(new { Message = "All KPI targets saved successfully." });
    }



    [HttpGet("procurement-kpi-target")]
    public async Task<IActionResult> GetProcurementKpiTargets(
    int companyId,
    int projectId,
    int targetYear,
    string? targetType = null)
    {
      var result = new List<KpiTargetDto>();

      using var connection = _context.Database.GetDbConnection();
      await connection.OpenAsync();

      using var command = connection.CreateCommand();
      command.CommandText = "dbPRCenter.dbo.GetProcurementKpiTargets";
      command.CommandType = CommandType.StoredProcedure;

      var p1 = command.CreateParameter();
      p1.ParameterName = "@CompanyId";
      p1.Value = companyId;
      p1.DbType = DbType.Int32;
      command.Parameters.Add(p1);

      var p2 = command.CreateParameter();
      p2.ParameterName = "@ProjectId";
      p2.Value = projectId;
      p2.DbType = DbType.Int32;
      command.Parameters.Add(p2);

      var p3 = command.CreateParameter();
      p3.ParameterName = "@TargetYear";
      p3.Value = targetYear;
      p3.DbType = DbType.Int32;
      command.Parameters.Add(p3);

 

      var p4 = command.CreateParameter();
      p4.ParameterName = "@TargetType";
      p4.Value = targetType ?? (object)DBNull.Value;
      p4.DbType = DbType.String;
      command.Parameters.Add(p4);

      using var reader = await command.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        result.Add(new KpiTargetDto
        {
          Id = Convert.ToInt32(reader["id"]),
          CompanyId = Convert.ToInt32(reader["company_id"]),
          ProjectId = Convert.ToInt32(reader["project_id"]),
          KpiName = reader["kpi_name"].ToString(),
          TargetType = reader["target_type"].ToString(),
          TargetMonth = Convert.ToInt32(reader["target_month"]),
          TargetYear = Convert.ToInt32(reader["target_year"]),
          TargetValue = Convert.ToDecimal(reader["target_value"]),
          Remarks = reader["remarks"].ToString(),
          created_by = Convert.ToInt32( reader["created_by"]),
          created_date = Convert.ToDateTime(reader["created_date"])
        });
      }

      return Ok(result);
    }



  }
}
