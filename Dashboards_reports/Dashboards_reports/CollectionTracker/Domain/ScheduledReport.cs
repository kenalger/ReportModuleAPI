namespace Dashboards_reports.CollectionTracker.Domain;

public sealed class ScheduledReport
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ReportType { get; set; } = "client-risk"; // client-risk, executive-portfolio, collection-performance, strategic-recommendations
    public string Frequency { get; set; } = "daily";       // daily, weekly, monthly
    public TimeSpan TimeOfDay { get; set; }
    public string? DaysOfWeek { get; set; }                 // "mon,wed,fri" (weekly only)
    public int? DayOfMonth { get; set; }                    // 1-28 (monthly only)
    public string Recipients { get; set; } = "[]";          // JSON array of emails
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
    public string? LastRunStatus { get; set; }
    public string? LastErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
