using System.Text.Json;
using Dashboards_reports.CollectionTracker.Domain;
using Dashboards_reports.CollectionTracker.Dtos;
using Dashboards_reports.CollectionTracker.Repositories;
using Dashboards_reports.CollectionTracker.Services;
using Microsoft.AspNetCore.Mvc;

using UserCtx = Dashboards_reports.CollectionTracker.Services.UserContext;

namespace Dashboards_reports.CollectionTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ScheduledReportsController(
    IScheduledReportRepository repository,
    IClientRepository clientRepository,
    IEmailService emailService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScheduledReportListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var reports = await repository.GetAllAsync(cancellationToken);
        return Ok(reports.Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ScheduledReportListItemDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var report = await repository.GetByIdAsync(id, cancellationToken);
        if (report is null)
            return NotFound(new { message = $"Schedule {id} was not found." });

        return Ok(ToDto(report));
    }

    [HttpPost]
    public async Task<ActionResult<ScheduledReportListItemDto>> Create(
        [FromBody] CreateScheduledReportRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateRequest(request.Name, request.Frequency, request.TimeOfDay,
            request.DaysOfWeek, request.DayOfMonth, request.Recipients);
        if (validation is not null) return validation;

        var entity = new ScheduledReport
        {
            Name = request.Name.Trim(),
            ReportType = string.IsNullOrWhiteSpace(request.ReportType) ? "client-risk" : request.ReportType,
            Frequency = request.Frequency.ToLowerInvariant(),
            TimeOfDay = TimeSpan.Parse(request.TimeOfDay),
            DaysOfWeek = request.DaysOfWeek is { Count: > 0 }
                ? string.Join(",", request.DaysOfWeek.Select(d => d.ToLowerInvariant()))
                : null,
            DayOfMonth = request.DayOfMonth,
            Recipients = JsonSerializer.Serialize(request.Recipients),
            ProjectId = request.ProjectId,
            IsActive = true,
            CreatedBy = UserCtx.GetUserDisplayName(HttpContext)
        };

        var id = await repository.CreateAsync(entity, cancellationToken);
        var created = await repository.GetByIdAsync(id, cancellationToken);
        return Ok(ToDto(created!));
    }

    [HttpPost("{id:int}/update")]
    public async Task<ActionResult<ScheduledReportListItemDto>> Update(
        int id, [FromBody] UpdateScheduledReportRequest request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound(new { message = $"Schedule {id} was not found." });

        var validation = ValidateRequest(request.Name, request.Frequency, request.TimeOfDay,
            request.DaysOfWeek, request.DayOfMonth, request.Recipients);
        if (validation is not null) return validation;

        var entity = new ScheduledReport
        {
            Name = request.Name.Trim(),
            ReportType = string.IsNullOrWhiteSpace(request.ReportType) ? "client-risk" : request.ReportType,
            Frequency = request.Frequency.ToLowerInvariant(),
            TimeOfDay = TimeSpan.Parse(request.TimeOfDay),
            DaysOfWeek = request.DaysOfWeek is { Count: > 0 }
                ? string.Join(",", request.DaysOfWeek.Select(d => d.ToLowerInvariant()))
                : null,
            DayOfMonth = request.DayOfMonth,
            Recipients = JsonSerializer.Serialize(request.Recipients),
            ProjectId = request.ProjectId
        };

        await repository.UpdateAsync(id, entity, cancellationToken);
        var updated = await repository.GetByIdAsync(id, cancellationToken);
        return Ok(ToDto(updated!));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new { message = $"Schedule {id} was not found." });

        return Ok(new { message = "Schedule deleted." });
    }

    [HttpPost("{id:int}/toggle")]
    public async Task<ActionResult> Toggle(int id, [FromBody] ToggleScheduleRequest request, CancellationToken cancellationToken)
    {
        var toggled = await repository.ToggleActiveAsync(id, request.IsActive, cancellationToken);
        if (!toggled)
            return NotFound(new { message = $"Schedule {id} was not found." });

        return Ok(new { message = request.IsActive ? "Schedule enabled." : "Schedule disabled." });
    }

    [HttpPost("{id:int}/run-now")]
    public async Task<ActionResult> RunNow(int id, CancellationToken cancellationToken)
    {
        var schedule = await repository.GetByIdAsync(id, cancellationToken);
        if (schedule is null)
            return NotFound(new { message = $"Schedule {id} was not found." });

        try
        {
            await ReportExecutor.ExecuteScheduleAsync(
                schedule, clientRepository, emailService, repository, cancellationToken);

            return Ok(new { message = "Report generated and sent successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Failed to run report: {ex.Message}" });
        }
    }

    /// <summary>
    /// Generate a report's HTML on demand (no email send). Used by the manual
    /// "Generate Report" buttons in ct-tracker. ReportType is one of:
    /// client-risk | executive-portfolio | collection-performance | strategic-recommendations | collection-forecast.
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<PreviewReportResponse>> Preview(
        [FromBody] PreviewReportRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReportType))
            return BadRequest(new { message = "ReportType is required." });

        try
        {
            var (html, note) = await ReportExecutor.GenerateReportAsync(
                request.ReportType, request.ProjectId, clientRepository, cancellationToken);

            return Ok(new PreviewReportResponse
            {
                Html = html,
                Note = note,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Failed to generate report: {ex.Message}" });
        }
    }

    private ActionResult? ValidateRequest(string name, string frequency, string timeOfDay,
        IReadOnlyList<string>? daysOfWeek, int? dayOfMonth, IReadOnlyList<string> recipients)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Name is required." });

        var validFreqs = new[] { "daily", "weekly", "monthly" };
        if (!validFreqs.Contains(frequency.ToLowerInvariant()))
            return BadRequest(new { message = "Frequency must be daily, weekly, or monthly." });

        if (!TimeSpan.TryParse(timeOfDay, out _))
            return BadRequest(new { message = "TimeOfDay must be a valid time (HH:mm)." });

        if (frequency.Equals("weekly", StringComparison.OrdinalIgnoreCase) &&
            (daysOfWeek is null || daysOfWeek.Count == 0))
            return BadRequest(new { message = "Days of week are required for weekly schedules." });

        if (frequency.Equals("monthly", StringComparison.OrdinalIgnoreCase) &&
            (dayOfMonth is null or < 1 or > 28))
            return BadRequest(new { message = "Day of month (1-28) is required for monthly schedules." });

        if (recipients.Count == 0)
            return BadRequest(new { message = "At least one recipient is required." });

        return null;
    }

    private static ScheduledReportListItemDto ToDto(ScheduledReport r)
    {
        var recipients = new List<string>();
        try { recipients = JsonSerializer.Deserialize<List<string>>(r.Recipients) ?? []; } catch { }

        var daysOfWeek = new List<string>();
        if (!string.IsNullOrWhiteSpace(r.DaysOfWeek))
            daysOfWeek = r.DaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        return new ScheduledReportListItemDto
        {
            Id = r.Id,
            Name = r.Name,
            ReportType = r.ReportType ?? "client-risk",
            Frequency = r.Frequency,
            TimeOfDay = r.TimeOfDay.ToString(@"hh\:mm"),
            DaysOfWeek = daysOfWeek,
            DayOfMonth = r.DayOfMonth,
            Recipients = recipients,
            ProjectId = r.ProjectId,
            ProjectName = r.ProjectName,
            IsActive = r.IsActive,
            LastRunAt = r.LastRunAt,
            LastRunStatus = r.LastRunStatus,
            LastErrorMessage = r.LastErrorMessage,
            CreatedAt = r.CreatedAt,
            CreatedBy = r.CreatedBy
        };
    }
}
