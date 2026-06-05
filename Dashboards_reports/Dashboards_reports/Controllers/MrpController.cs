using Dashboards_reports.Data;
using Dashboards_reports.Models.MaterialPlanning;
using Dashboards_reports.Models.MaterialPlanning.DTO;
using Dashboards_reports.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dashboards_reports.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MrpController : ControllerBase
    {
        private readonly InventoryDbContext _context;
      
        private readonly MRPService _mrpService;

        public MrpController(InventoryDbContext context, MRPService mrpService)
        {
            _context = context;
            _mrpService = mrpService;
        }

        [HttpGet("GetMPS")]
        public IActionResult Get(int companyId, int warehouseId, int materialId)
        {
            var setup = _context.MaterialPlanningSetups
                .FirstOrDefault(x =>
                    x.CompanyId == companyId &&
                    x.WarehouseId == warehouseId &&
                    x.MaterialId == materialId);

            if (setup == null)
                return NotFound();

            return Ok(setup);
        }

        [HttpPost("CreateMPS")]
        public IActionResult Create(MaterialPlanningSetup dto)
        {
            var exists = _context.MaterialPlanningSetups.Any(x =>
                x.CompanyId == dto.CompanyId &&
                x.WarehouseId == dto.WarehouseId &&
                x.MaterialId == dto.MaterialId);

            if (exists)
                return BadRequest("Planning setup already exists.");

            _context.MaterialPlanningSetups.Add(dto);
            _context.SaveChanges();

            return Ok(dto);
        }

        [HttpPut("UpdateMPS/{id}")]
        public IActionResult Update(int id, MaterialPlanningSetup dto)
        {
            var setup = _context.MaterialPlanningSetups.Find(id);
            if (setup == null)
                return NotFound();

            setup.MinimumOrderQty = dto.MinimumOrderQty;
            setup.SafetyStock = dto.SafetyStock;
            setup.LeadTimeWeeks = dto.LeadTimeWeeks;
            setup.UpdatedDate = DateTime.Now;

            _context.SaveChanges();
            return Ok(setup);
        }

        [HttpDelete("DeleteMPS/{id}")]
        public IActionResult Delete(int id)
        {
            var setup = _context.MaterialPlanningSetups.Find(id);
            if (setup == null)
                return NotFound();

            _context.MaterialPlanningSetups.Remove(setup);
            _context.SaveChanges();

            return Ok();
        }

        [HttpGet("setup")]
        public IActionResult GetPlanningSetup(
            int companyId,
            int warehouseId,
            int materialId)
                {
                    var setup = _context.MaterialPlanningSetups
                        .FirstOrDefault(x =>
                            x.CompanyId == companyId &&
                            x.WarehouseId == warehouseId &&
                            x.MaterialId == materialId);

                    if (setup == null)
                        return NotFound("Planning setup not found.");

                        // 🔹 LIVE inventory lookup
                        var inventory = _context.InventoryItems
                            .FirstOrDefault(x =>
                                x.LocationId == warehouseId &&
                                x.ItemId == materialId);

                        decimal onHandQty = inventory?.Quantity ?? 0;

            return Ok(new
                    {
                        minimumOrderQty = setup.MinimumOrderQty,
                        safetyStock = setup.SafetyStock,
                        onHandQty = onHandQty,
                        leadTimeWeeks = setup.LeadTimeWeeks
                       
            });
                }


        [HttpPost("run")]
        public IActionResult RunMRP([FromBody] MRPRunRequestDto request)
        {
            var setup = _context.MaterialPlanningSetups.FirstOrDefault(x =>
                x.CompanyId == request.CompanyId &&
                x.WarehouseId == request.WarehouseId &&
                x.MaterialId == request.MaterialId);

            if (setup == null)
                return BadRequest("Planning setup not found.");

            // Replace with real inventory lookup
            //decimal onHandQty = 2500;
            var inventory = _context.InventoryItems.FirstOrDefault(x =>
               // x.LocationId == request.CompanyId &&
                x.LocationId == request.WarehouseId &&
                x.ItemId == request.MaterialId);

            decimal onHandQty = inventory?.Quantity ?? 0;


            var calculated = _mrpService.Run(
                onHandQty,
                setup.SafetyStock,
                setup.MinimumOrderQty,
                setup.LeadTimeWeeks,
                request.Weeks);

            var run = new MRPRun
            {
                CompanyId = request.CompanyId,
                WarehouseId = request.WarehouseId,
                MaterialId = request.MaterialId,
                StartWeek = request.StartWeek,
                EndWeek = request.EndWeek,
                RunDate = DateTime.Now,
                Details = calculated.Select(x => new MRPRunDetail
                {
                    WeekNo = x.WeekNo,
                    GrossRequirements = x.GrossRequirements,
                    ScheduledReceipts = x.ScheduledReceipts,
                    ProjectedAvailableBalance = x.ProjectedAvailableBalance,
                    NetRequirements = x.NetRequirements,
                    PlannedOrderReceipt = x.PlannedOrderReceipt,
                    PlannedOrderRelease = x.PlannedOrderRelease
                }).ToList()
            };

            _context.MRPRuns.Add(run);
            _context.SaveChanges();

            return Ok(calculated);

            //return Ok(new
            //{
            //    runId = run.MRPRunId,
            //    results = calculated
            //});
        }

        [HttpGet("history")]
        public IActionResult GetMRPHistory(
            [FromQuery] int companyId,
            [FromQuery] int warehouseId,
            [FromQuery] int materialId)
        {
            var runs = _context.MRPRuns
                .Where(x =>
                    x.CompanyId == companyId &&
                    x.WarehouseId == warehouseId &&
                    x.MaterialId == materialId)
                .OrderByDescending(x => x.RunDate)
                .Select(x => new MRPRunHistoryHeaderDto
                {
                    MRPRunId = x.MRPRunId,
                    CompanyId = x.CompanyId,
                    WarehouseId = x.WarehouseId,
                    MaterialId = x.MaterialId,
                    StartWeek = x.StartWeek,
                    EndWeek = x.EndWeek,
                    RunDate = x.RunDate
                })
                .ToList();

            return Ok(runs);
        }

        [HttpGet("history/{mrpRunId}")]
        public IActionResult GetMRPRunDetails(int mrpRunId)
        {
            var details = _context.MRPRunDetails
                .Where(x => x.MRPRunId == mrpRunId)
                .OrderBy(x => x.WeekNo)
                .Select(x => new MRPRunHistoryDetailDto
                {
                    WeekNo = x.WeekNo,
                    GrossRequirements = x.GrossRequirements,
                    ScheduledReceipts = x.ScheduledReceipts,
                    ProjectedAvailableBalance = x.ProjectedAvailableBalance,
                    NetRequirements = x.NetRequirements,
                    PlannedOrderReceipt = x.PlannedOrderReceipt,
                    PlannedOrderRelease = x.PlannedOrderRelease
                })
                .ToList();

            if (!details.Any())
                return NotFound("MRP run not found.");

            return Ok(details);
        }


    }
}
