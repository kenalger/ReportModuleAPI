using Dashboards_reports.CollectionTracker.Dtos;
using Dashboards_reports.CollectionTracker.Repositories;
using Dashboards_reports.CollectionTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dashboards_reports.CollectionTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController(IClientRepository repository) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromQuery] int? projectId = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var allClients = await repository.GetClientsAsync(cancellationToken);
        var clients = projectId.HasValue
            ? allClients.Where(c => c.ProjectId == projectId.Value).ToList()
            : allClients;

        var resolved = clients.Where(ClientMetrics.IsResolved).ToList();
        var cancelled = clients.Where(ClientMetrics.IsCancelled).ToList();
        var active = clients.Where(c => !ClientMetrics.IsResolved(c) && !ClientMetrics.IsCancelled(c)).ToList();
        var delayed = clients.Where(ClientMetrics.HasActiveDelayReason).ToList();
        var resolvedWithDates = resolved.Where(c => c.ResolvedDate.HasValue).ToList();

        var topDelayReason = clients
            .SelectMany(ClientMetrics.DelayReasons)
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "-";

        var summary = new DashboardSummaryDto
        {
            TotalClients = clients.Count,
            ResolvedClients = resolved.Count,
            ActiveClients = active.Count,
            CriticalClients = active.Count(c => ClientMetrics.AgingStatus(c, today) == "critical"),
            WarningClients = active.Count(c => ClientMetrics.AgingStatus(c, today) == "warning"),
            WatchClients = active.Count(c => ClientMetrics.AgingStatus(c, today) == "watch"),
            OnTrackClients = active.Count(c => ClientMetrics.AgingStatus(c, today) == "ok"),
            OverdueClients = active.Count(c => c.TargetDate.HasValue && c.TargetDate.Value.Date < today),
            CompletionRatePercent = clients.Count == 0
                ? 0
                : (int)Math.Round(resolved.Count / (double)clients.Count * 100d, MidpointRounding.AwayFromZero),
            AverageDelayedDays = delayed.Count == 0
                ? 0
                : (int)Math.Round(delayed.Average(c => ClientMetrics.TotalDays(c, today)), MidpointRounding.AwayFromZero),
            AverageResolutionDays = resolvedWithDates.Count == 0
                ? 0
                : (int)Math.Round(resolvedWithDates.Average(c => ClientMetrics.TotalDays(c, today)), MidpointRounding.AwayFromZero),
            FastestResolutionDays = resolvedWithDates.Count == 0
                ? null
                : resolvedWithDates.Min(c => ClientMetrics.TotalDays(c, today)),
            TopDelayReason = topDelayReason
        };

        return Ok(summary);
    }

    [HttpGet("breakdown")]
    public async Task<ActionResult<DashboardBreakdownDto>> GetBreakdown(
        [FromQuery] int top = 6,
        [FromQuery] int? projectId = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var allClients = await repository.GetClientsAsync(cancellationToken);
        var clients = projectId.HasValue
            ? allClients.Where(c => c.ProjectId == projectId.Value).ToList()
            : allClients;
        var stageOrder = (await repository.GetStagesAsync(cancellationToken))
            .Select(s => s.Name)
            .ToList();
        top = top <= 0 ? 6 : Math.Min(top, 25);

        var delayReasons = clients
            .SelectMany(ClientMetrics.DelayReasons)
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(g => new BreakdownItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label)
            .Take(top)
            .ToList();

        var stages = clients
            .GroupBy(c => c.Stage, StringComparer.OrdinalIgnoreCase)
            .Select(g => new BreakdownItemDto(g.First().Stage, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => ClientMetrics.StageRank(x.Label, stageOrder))
            .Take(top)
            .ToList();

        var financingMix = clients
            .GroupBy(c => c.FinancingType, StringComparer.OrdinalIgnoreCase)
            .Select(g => new BreakdownItemDto(g.First().FinancingType, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label)
            .ToList();

        var resolutionByFinancing = clients
            .Where(c => c.ResolvedDate.HasValue)
            .GroupBy(c => c.FinancingType, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var avgDays = (int)Math.Round(g.Average(c => ClientMetrics.TotalDays(c, today)), MidpointRounding.AwayFromZero);
                return new ResolutionBreakdownDto(g.First().FinancingType, avgDays, g.Count());
            })
            .OrderByDescending(x => x.AverageDays)
            .ThenBy(x => x.FinancingType)
            .ToList();

        var result = new DashboardBreakdownDto
        {
            DelayReasons = delayReasons,
            Stages = stages,
            FinancingMix = financingMix,
            ResolutionByFinancing = resolutionByFinancing
        };

        return Ok(result);
    }
}
