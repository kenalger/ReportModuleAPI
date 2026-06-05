using Dashboards_reports.CollectionTracker.Domain;

namespace Dashboards_reports.CollectionTracker.Repositories;

public interface IKpiTargetRepository
{
    Task<KpiTarget> GetAsync(CancellationToken cancellationToken);

    Task<KpiTarget> UpsertAsync(
        int stuckThresholdDays,
        decimal stuckRateTargetPercent,
        int loanCycleTargetDays,
        string? updatedBy,
        CancellationToken cancellationToken);
}
