using Dashboards_reports.CollectionTracker.Dtos;
using Dashboards_reports.CollectionTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dashboards_reports.CollectionTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InsightsController : ControllerBase
{
    private readonly IEmailService _emailService;

    public InsightsController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("send-report")]
    public async Task<IActionResult> SendReport(
        [FromBody] InsightsEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Recipients.Count == 0)
        {
            return BadRequest(new { message = "At least one recipient is required." });
        }

        if (string.IsNullOrWhiteSpace(request.BodyHtml))
        {
            return BadRequest(new { message = "Report body is required." });
        }

        await _emailService.SendHtmlEmailAsync(
            request.Recipients,
            string.IsNullOrWhiteSpace(request.Subject)
                ? "Collection Tracker - Client Analysis Report"
                : request.Subject,
            request.BodyHtml,
            cancellationToken);

        return Ok(new { message = "Report sent successfully." });
    }
}
