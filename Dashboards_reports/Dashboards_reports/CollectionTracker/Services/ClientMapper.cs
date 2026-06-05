using Dashboards_reports.CollectionTracker.Domain;
using Dashboards_reports.CollectionTracker.Dtos;

namespace Dashboards_reports.CollectionTracker.Services;

public static class ClientMapper
{
    public static ClientListItemDto ToListItem(Client client, DateTime today, IReadOnlyList<string>? stageOrder = null)
    {
        var days = ClientMetrics.AgingDays(client, today);
        var aging = ClientMetrics.AgingStatus(client, today);
        var isResolved = ClientMetrics.IsResolved(client);
        var isCancelled = ClientMetrics.IsCancelled(client);
        var pendingTasks = client.Tasks.Where(t => !t.IsDone).ToList();
        var overdueTaskCount = pendingTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date < today.Date);
        var nextTask = ClientMetrics.SelectNextActionTask(client.Tasks);
        var delayReasons = GetDelayReasons(client);
        var primaryDelayReason = delayReasons.FirstOrDefault() ?? "None";
        var secondaryDelayReason = delayReasons.Skip(1).FirstOrDefault();

        return new ClientListItemDto
        {
            Id = client.Id,
            Name = client.Name,
            UnitId = client.UnitId,
            Unit = client.Unit,
            ProjectId = client.ProjectId,
            ProjectName = client.ProjectName,
            TotalContractPrice = client.TotalContractPrice,
            ContactNumber = client.ContactNumber,
            BrokerName = client.BrokerName,
            FinancingType = client.FinancingType,
            Stage = client.Stage,
            StageDate = client.StageDate,
            TargetDate = client.TargetDate,
            ResolvedDate = client.ResolvedDate,
            DelayReasons = delayReasons,
            DelayReason = primaryDelayReason,
            SecondaryDelayReason = secondaryDelayReason,
            NextAction = nextTask?.Description ?? client.NextAction,
            FollowUpDate = nextTask?.DueDate ?? client.FollowUpDate,
            Notes = client.Notes,
            AddedDate = client.AddedDate,
            CreatedBy = client.CreatedBy,
            ModifiedBy = client.ModifiedBy,
            ResolvedHow = client.ResolvedHow,
            ResolvedNotes = client.ResolvedNotes,
            DaysInStage = days,
            ProgressPercent = ClientMetrics.StageProgress(client.Stage, stageOrder),
            AgingStatus = aging,
            IsResolved = isResolved,
            IsCancelled = isCancelled,
            IsTargetOverdue = client.TargetDate.HasValue && client.TargetDate.Value.Date < today.Date && !isResolved && !isCancelled,
            ActivityCount = client.Activities.Count,
            PendingTaskCount = pendingTasks.Count,
            OverdueTaskCount = overdueTaskCount
        };
    }

    public static ClientDetailDto ToDetail(Client client, DateTime today, IReadOnlyList<string>? stageOrder = null)
    {
        var list = ToListItem(client, today, stageOrder);

        return new ClientDetailDto
        {
            Id = list.Id,
            Name = list.Name,
            UnitId = list.UnitId,
            Unit = list.Unit,
            ProjectId = list.ProjectId,
            ProjectName = list.ProjectName,
            TotalContractPrice = list.TotalContractPrice,
            ContactNumber = list.ContactNumber,
            BrokerName = list.BrokerName,
            FinancingType = list.FinancingType,
            Stage = list.Stage,
            StageDate = list.StageDate,
            TargetDate = list.TargetDate,
            ResolvedDate = list.ResolvedDate,
            DelayReason = list.DelayReason,
            SecondaryDelayReason = list.SecondaryDelayReason,
            NextAction = list.NextAction,
            FollowUpDate = list.FollowUpDate,
            Notes = list.Notes,
            AddedDate = list.AddedDate,
            CreatedBy = list.CreatedBy,
            ModifiedBy = list.ModifiedBy,
            ResolvedHow = list.ResolvedHow,
            ResolvedNotes = list.ResolvedNotes,
            DaysInStage = list.DaysInStage,
            ProgressPercent = list.ProgressPercent,
            AgingStatus = list.AgingStatus,
            IsResolved = list.IsResolved,
            IsCancelled = list.IsCancelled,
            IsTargetOverdue = list.IsTargetOverdue,
            ActivityCount = list.ActivityCount,
            PendingTaskCount = list.PendingTaskCount,
            OverdueTaskCount = list.OverdueTaskCount,
            TotalDays = ClientMetrics.TotalDays(client, today),
            Activities = client.Activities
                .OrderByDescending(a => a.ActivityDateTime)
                .Select(a => new ActivityDto
                {
                    Id = a.Id,
                    ClientId = a.ClientId,
                    ActivityType = a.ActivityType,
                    Description = a.Description,
                    ActivityDateTime = a.ActivityDateTime,
                    CreatedAt = a.CreatedAt
                })
                .ToList(),
            Tasks = client.Tasks
                .OrderBy(t => t.IsDone)
                .ThenBy(t => t.DueDate.HasValue ? 0 : 1)
                .ThenBy(t => t.DueDate)
                .ThenBy(t => ClientMetrics.PriorityRank(t.Priority))
                .Select(t => new TaskDto
                {
                    Id = t.Id,
                    ClientId = t.ClientId,
                    Description = t.Description,
                    DueDate = t.DueDate,
                    Priority = t.Priority,
                    AssignedTo = t.AssignedTo,
                    IsDone = t.IsDone,
                    DoneAt = t.DoneAt,
                    AddedAt = t.AddedAt
                })
                .ToList()
        };
    }

    private static IReadOnlyList<string> GetDelayReasons(Client client)
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

        var fromColumns = new[] { client.DelayReason, client.SecondaryDelayReason }
            .Where(reason => !string.IsNullOrWhiteSpace(reason) && !string.Equals(reason, "None", StringComparison.OrdinalIgnoreCase))
            .Select(reason => reason!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return fromColumns;
    }
}
