using Dashboards_reports.CollectionTracker.Domain;

namespace Dashboards_reports.CollectionTracker.Repositories;

public interface IScheduledReportRepository
{
    Task<IReadOnlyList<ScheduledReport>> GetAllAsync(CancellationToken cancellationToken);
    Task<ScheduledReport?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<int> CreateAsync(ScheduledReport report, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int id, ScheduledReport report, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<bool> ToggleActiveAsync(int id, bool isActive, CancellationToken cancellationToken);
    Task UpdateRunStatusAsync(int id, DateTime runAt, string status, string? errorMessage, CancellationToken cancellationToken);
    Task<IReadOnlyList<ScheduledReport>> GetDueSchedulesAsync(CancellationToken cancellationToken);
}
