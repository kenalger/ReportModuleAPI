using Dashboards_reports.CollectionTracker.Domain;
using Dashboards_reports.CollectionTracker.Dtos;
using Dashboards_reports.CollectionTracker.Repositories;
using Dashboards_reports.CollectionTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dashboards_reports.CollectionTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class KpiTargetsController(IKpiTargetRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<KpiTargetDto>> GetKpiTargets(CancellationToken cancellationToken)
    {
        var target = await repository.GetAsync(cancellationToken);
        return Ok(ToDto(target));
    }

    [HttpPost]
    public async Task<ActionResult<KpiTargetDto>> UpdateKpiTargets(
        [FromBody] UpdateKpiTargetsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StuckThresholdDays < 1 || request.StuckThresholdDays > 365)
        {
            return BadRequest(new { message = "Stuck threshold must be between 1 and 365 days." });
        }

        if (request.StuckRateTargetPercent < 0 || request.StuckRateTargetPercent > 100)
        {
            return BadRequest(new { message = "Stuck rate target must be between 0 and 100%." });
        }

        if (request.LoanCycleTargetDays < 1 || request.LoanCycleTargetDays > 365)
        {
            return BadRequest(new { message = "Loan cycle target must be between 1 and 365 days." });
        }

        var updatedBy = UserContext.GetUserDisplayName(HttpContext)
            ?? UserContext.GetUserId(HttpContext);

        var saved = await repository.UpsertAsync(
            request.StuckThresholdDays,
            request.StuckRateTargetPercent,
            request.LoanCycleTargetDays,
            updatedBy,
            cancellationToken);

        return Ok(ToDto(saved));
    }

    private static KpiTargetDto ToDto(KpiTarget target) =>
        new()
        {
            StuckThresholdDays = target.StuckThresholdDays,
            StuckRateTargetPercent = target.StuckRateTargetPercent,
            LoanCycleTargetDays = target.LoanCycleTargetDays,
            UpdatedAt = target.UpdatedAt,
            UpdatedBy = target.UpdatedBy
        };
}
