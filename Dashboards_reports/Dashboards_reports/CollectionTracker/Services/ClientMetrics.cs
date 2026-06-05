using Dashboards_reports.CollectionTracker.Domain;

namespace Dashboards_reports.CollectionTracker.Services;

public static class ClientMetrics
{
    private static readonly string[] DefaultStageOrder =
    [
        "Reservation",
        "Equity Collection",
        "Loan Application",
        "Document Submission",
        "Bank/PI Evaluation",
        "Loan Approval",
        "Mortgage Signing",
        "Takeout Processing",
        "Proceeds Released",
        "Resolved"
    ];

    public static bool IsResolved(Client client) =>
        string.Equals(client.Stage, "Resolved", StringComparison.OrdinalIgnoreCase);

    public static bool IsCancelled(Client client) =>
        string.Equals(client.Stage, "Cancellation", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(client.Stage, "Pending Cancellation", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(client.Stage, "Loan Status: Declined", StringComparison.OrdinalIgnoreCase);

    public static int AgingDays(Client client, DateTime today)
    {
        if (!client.TargetDate.HasValue)
        {
            return 0;
        }

        var target = client.TargetDate.Value.Date;

        if ((IsResolved(client) || IsCancelled(client)) && client.ResolvedDate.HasValue)
        {
            return Math.Max(0, (client.ResolvedDate.Value.Date - target).Days);
        }

        return Math.Max(0, (today.Date - target).Days);
    }

    public static int TotalDays(Client client, DateTime today)
    {
        var start = client.AddedDate.Date;
        var end = client.ResolvedDate?.Date ?? today.Date;
        return Math.Max(0, (end - start).Days);
    }

    public static string AgingStatus(Client client, DateTime today)
    {
        if (IsResolved(client) || IsCancelled(client))
        {
            return "ok";
        }

        var days = AgingDays(client, today);

        if (days >= 60)
        {
            return "critical";
        }

        if (days >= 30)
        {
            return "warning";
        }

        if (days >= 14)
        {
            return "watch";
        }

        return "ok";
    }

    public static int StageProgress(string? stage, IReadOnlyList<string>? stageOrder = null)
    {
        var order = stageOrder is { Count: > 0 } ? stageOrder : DefaultStageOrder;
        var progressOrder = order
            .Where(s => !string.Equals(s, "Cancellation", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var index = IndexOfStage(progressOrder, stage);
        if (index < 0)
        {
            return 0;
        }

        return (int)Math.Round((index + 1) / (double)progressOrder.Count * 100d, MidpointRounding.AwayFromZero);
    }

    public static int StageRank(string? stage, IReadOnlyList<string>? stageOrder = null)
    {
        var order = stageOrder is { Count: > 0 } ? stageOrder : DefaultStageOrder;
        var index = IndexOfStage(order, stage);
        return index < 0 ? int.MaxValue : index;
    }

    public static IReadOnlyList<string> DelayReasons(Client client)
    {
        var fromTable = client.DelayReasons
            .Where(reason => !string.IsNullOrWhiteSpace(reason) && !string.Equals(reason, "None", StringComparison.OrdinalIgnoreCase))
            .Select(reason => reason.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (fromTable.Count > 0)
        {
            return fromTable;
        }

        return new[] { client.DelayReason, client.SecondaryDelayReason }
            .Where(reason => !string.IsNullOrWhiteSpace(reason) && !string.Equals(reason, "None", StringComparison.OrdinalIgnoreCase))
            .Select(reason => reason!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool HasActiveDelayReason(Client client) => DelayReasons(client).Count > 0;

    private static int IndexOfStage(IReadOnlyList<string> stageOrder, string? stage)
    {
        for (var i = 0; i < stageOrder.Count; i++)
        {
            if (string.Equals(stageOrder[i], stage, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public static TaskItem? SelectNextActionTask(IEnumerable<TaskItem> tasks) =>
        tasks
            .Where(t => !t.IsDone)
            .OrderBy(t => t.DueDate.HasValue ? 0 : 1)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => PriorityRank(t.Priority))
            .ThenBy(t => t.Id)
            .FirstOrDefault();

    public static int PriorityRank(string? priority) =>
        priority?.Trim().ToLowerInvariant() switch
        {
            "high" => 0,
            "medium" => 1,
            "low" => 2,
            _ => 1
        };
}
