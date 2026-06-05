namespace Dashboards_reports.CollectionTracker.Dtos;

public sealed record CreateScheduledReportRequest
{
    public string Name { get; init; } = string.Empty;
    public string ReportType { get; init; } = "client-risk";
    public string Frequency { get; init; } = "daily";
    public string TimeOfDay { get; init; } = "08:00";       // HH:mm
    public IReadOnlyList<string>? DaysOfWeek { get; init; }  // ["mon","wed","fri"]
    public int? DayOfMonth { get; init; }
    public IReadOnlyList<string> Recipients { get; init; } = [];
    public int? ProjectId { get; init; }
    public string? CreatedBy { get; init; }
}

public sealed record UpdateScheduledReportRequest
{
    public string Name { get; init; } = string.Empty;
    public string ReportType { get; init; } = "client-risk";
    public string Frequency { get; init; } = "daily";
    public string TimeOfDay { get; init; } = "08:00";
    public IReadOnlyList<string>? DaysOfWeek { get; init; }
    public int? DayOfMonth { get; init; }
    public IReadOnlyList<string> Recipients { get; init; } = [];
    public int? ProjectId { get; init; }
}

public sealed record ToggleScheduleRequest
{
    public bool IsActive { get; init; }
}

public sealed record PreviewReportRequest
{
    public string ReportType { get; init; } = "client-risk";
    public int? ProjectId { get; init; }
}

public sealed record PreviewReportResponse
{
    public string? Html { get; init; }
    public string? Note { get; init; }
    public DateTime GeneratedAt { get; init; }
}

public sealed record ScheduledReportListItemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ReportType { get; init; } = "client-risk";
    public string Frequency { get; init; } = string.Empty;
    public string TimeOfDay { get; init; } = string.Empty;
    public IReadOnlyList<string> DaysOfWeek { get; init; } = [];
    public int? DayOfMonth { get; init; }
    public IReadOnlyList<string> Recipients { get; init; } = [];
    public int? ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastRunAt { get; init; }
    public string? LastRunStatus { get; init; }
    public string? LastErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}
