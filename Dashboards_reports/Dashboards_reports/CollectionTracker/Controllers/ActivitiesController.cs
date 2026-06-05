using Dashboards_reports.CollectionTracker.Dtos;
using Dashboards_reports.CollectionTracker.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Dashboards_reports.CollectionTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ActivitiesController(IClientRepository repository) : ControllerBase
{
    [HttpGet("recent")]
    public async Task<ActionResult<IReadOnlyList<RecentActivityDto>>> GetRecentActivities(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1) limit = 50;
        if (limit > 200) limit = 200;
        if (offset < 0) offset = 0;

        var activities = await repository.GetRecentActivitiesAsync(limit, offset, cancellationToken);
        return Ok(activities);
    }
}
