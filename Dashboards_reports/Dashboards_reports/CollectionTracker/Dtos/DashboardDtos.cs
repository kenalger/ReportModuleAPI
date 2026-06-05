namespace Dashboards_reports.CollectionTracker.Dtos;

public sealed record DashboardSummaryDto
{
    public int TotalClients { get; init; }
    public int ResolvedClients { get; init; }
    public int ActiveClients { get; init; }
    public int CriticalClients { get; init; }
    public int WarningClients { get; init; }
    public int WatchClients { get; init; }
    public int OnTrackClients { get; init; }
    public int OverdueClients { get; init; }
    public int CompletionRatePercent { get; init; }
    public int AverageDelayedDays { get; init; }
    public int AverageResolutionDays { get; init; }
    public int? FastestResolutionDays { get; init; }
    public string TopDelayReason { get; init; } = "-";
}

public sealed record BreakdownItemDto(string Label, int Count);

public sealed record ResolutionBreakdownDto(string FinancingType, int AverageDays, int ResolvedCount);

public sealed record DashboardBreakdownDto
{
    public IReadOnlyList<BreakdownItemDto> DelayReasons { get; init; } = [];
    public IReadOnlyList<BreakdownItemDto> Stages { get; init; } = [];
    public IReadOnlyList<BreakdownItemDto> FinancingMix { get; init; } = [];
    public IReadOnlyList<ResolutionBreakdownDto> ResolutionByFinancing { get; init; } = [];
}
