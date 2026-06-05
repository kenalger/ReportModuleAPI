using Dashboards_reports.CollectionTracker.Repositories;

namespace Dashboards_reports.CollectionTracker.Services;

public sealed class ReportSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReportSchedulerService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    public ReportSchedulerService(IServiceProvider serviceProvider, ILogger<ReportSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReportSchedulerService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRunDueSchedulesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReportSchedulerService tick.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckAndRunDueSchedulesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleRepo = scope.ServiceProvider.GetRequiredService<IScheduledReportRepository>();
        var clientRepo = scope.ServiceProvider.GetRequiredService<IClientRepository>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var dueSchedules = await scheduleRepo.GetDueSchedulesAsync(cancellationToken);

        if (dueSchedules.Count == 0) return;

        _logger.LogInformation("Found {Count} due schedule(s) to execute.", dueSchedules.Count);

        foreach (var schedule in dueSchedules)
        {
            if (!ShouldRunToday(schedule)) continue;

            try
            {
                _logger.LogInformation("Executing schedule '{Name}' (Id={Id})", schedule.Name, schedule.Id);

                await ReportExecutor.ExecuteScheduleAsync(
                    schedule, clientRepo, email, scheduleRepo, cancellationToken);

                _logger.LogInformation("Schedule '{Name}' executed successfully.", schedule.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute schedule '{Name}' (Id={Id})", schedule.Name, schedule.Id);
            }
        }
    }

    private static bool ShouldRunToday(Domain.ScheduledReport schedule)
    {
        var today = DateTime.Now;

        return schedule.Frequency.ToLowerInvariant() switch
        {
            "daily" => true,
            "weekly" => ShouldRunWeekly(schedule.DaysOfWeek, today),
            "monthly" => schedule.DayOfMonth.HasValue && today.Day == schedule.DayOfMonth.Value,
            _ => false
        };
    }

    private static bool ShouldRunWeekly(string? daysOfWeek, DateTime today)
    {
        if (string.IsNullOrWhiteSpace(daysOfWeek)) return false;

        var todayAbbr = today.DayOfWeek switch
        {
            DayOfWeek.Monday => "mon",
            DayOfWeek.Tuesday => "tue",
            DayOfWeek.Wednesday => "wed",
            DayOfWeek.Thursday => "thu",
            DayOfWeek.Friday => "fri",
            DayOfWeek.Saturday => "sat",
            DayOfWeek.Sunday => "sun",
            _ => ""
        };

        var days = daysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return days.Any(d => d.Equals(todayAbbr, StringComparison.OrdinalIgnoreCase));
    }
}
