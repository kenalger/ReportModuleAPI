using Dashboards_reports.CollectionTracker.Domain;
using Dashboards_reports.CollectionTracker.Dtos;

namespace Dashboards_reports.CollectionTracker.Repositories;

public interface IClientRepository
{
    Task<IReadOnlyList<Client>> GetClientsAsync(CancellationToken cancellationToken);
    Task<Client?> GetClientByIdAsync(int clientId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StageDefinition>> GetStagesAsync(CancellationToken cancellationToken);
    Task<StageDefinition?> GetStageByIdAsync(int stageId, CancellationToken cancellationToken);
    Task<int> CreateStageAsync(string name, CancellationToken cancellationToken);
    Task<bool> UpdateStageAsync(int stageId, string name, CancellationToken cancellationToken);
    Task<bool> DeleteStageAsync(
        int stageId,
        string stageName,
        string? replacementStageName,
        CancellationToken cancellationToken);
    Task<bool> IsStageUsedAsync(string stageName, CancellationToken cancellationToken);
    Task ReorderStagesAsync(List<int> stageIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<DelayReasonDefinition>> GetDelayReasonsAsync(CancellationToken cancellationToken);
    Task<DelayReasonDefinition?> GetDelayReasonByIdAsync(int delayReasonId, CancellationToken cancellationToken);
    Task<int> CreateDelayReasonAsync(string name, CancellationToken cancellationToken);
    Task<bool> UpdateDelayReasonAsync(int delayReasonId, string name, CancellationToken cancellationToken);
    Task<bool> DeleteDelayReasonAsync(
        int delayReasonId,
        string delayReasonName,
        string replacementDelayReasonName,
        CancellationToken cancellationToken);
    Task<bool> IsDelayReasonUsedAsync(string delayReasonName, CancellationToken cancellationToken);
    Task<IReadOnlyList<FinancingTypeDefinition>> GetFinancingTypesAsync(CancellationToken cancellationToken);
    Task<FinancingTypeDefinition?> GetFinancingTypeByIdAsync(int financingTypeId, CancellationToken cancellationToken);
    Task<int> CreateFinancingTypeAsync(string name, CancellationToken cancellationToken);
    Task<bool> UpdateFinancingTypeAsync(int financingTypeId, string name, CancellationToken cancellationToken);
    Task<bool> DeleteFinancingTypeAsync(
        int financingTypeId,
        string financingTypeName,
        string replacementFinancingTypeName,
        CancellationToken cancellationToken);
    Task<bool> IsFinancingTypeUsedAsync(string financingTypeName, CancellationToken cancellationToken);
    Task<IReadOnlyList<ActivityTypeDefinition>> GetActivityTypesAsync(CancellationToken cancellationToken);
    Task<ActivityTypeDefinition?> GetActivityTypeByIdAsync(int activityTypeId, CancellationToken cancellationToken);
    Task<int> CreateActivityTypeAsync(string code, string label, CancellationToken cancellationToken);
    Task<bool> UpdateActivityTypeAsync(int activityTypeId, string code, string label, CancellationToken cancellationToken);
    Task<bool> DeleteActivityTypeAsync(
        int activityTypeId,
        string activityTypeCode,
        string replacementActivityTypeCode,
        CancellationToken cancellationToken);
    Task<bool> IsActivityTypeUsedAsync(string activityTypeCode, CancellationToken cancellationToken);
    Task<int> CreateClientAsync(UpsertClientRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateClientAsync(int clientId, UpsertClientRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteClientAsync(int clientId, CancellationToken cancellationToken);
    Task<bool> ResolveClientAsync(int clientId, ResolveClientRequest request, string? modifiedBy, CancellationToken cancellationToken);
    Task<int> AddActivityAsync(int clientId, AddActivityRequest request, string? createdBy, CancellationToken cancellationToken);
    Task<bool> DeleteActivityAsync(int clientId, int activityId, CancellationToken cancellationToken);
    Task<int> AddTaskAsync(int clientId, AddTaskRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateTaskStatusAsync(int clientId, int taskId, bool isDone, CancellationToken cancellationToken);
    Task<bool> DeleteTaskAsync(int clientId, int taskId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Project>> GetProjectsAsync(CancellationToken cancellationToken);
    Task<Project?> GetProjectByIdAsync(int projectId, CancellationToken cancellationToken);
    Task<int> CreateProjectAsync(string name, CancellationToken cancellationToken);
    Task<bool> UpdateProjectAsync(int projectId, string name, CancellationToken cancellationToken);
    Task<bool> DeleteProjectAsync(int projectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProjectUnit>> GetProjectUnitsAsync(int projectId, CancellationToken cancellationToken);
    Task<ProjectUnit?> GetProjectUnitByIdAsync(int projectId, int unitId, CancellationToken cancellationToken);
    Task<ProjectUnit?> GetProjectUnitByUnitIdAsync(int unitId, CancellationToken cancellationToken);
    Task<int> CreateProjectUnitAsync(int projectId, string name, decimal? totalContractPrice, CancellationToken cancellationToken);
    Task<bool> UpdateProjectUnitAsync(int projectId, int unitId, string name, decimal? totalContractPrice, CancellationToken cancellationToken);
    Task<bool> DeleteProjectUnitAsync(int projectId, int unitId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecentActivityDto>> GetRecentActivitiesAsync(int limit, int offset, CancellationToken cancellationToken);

    // ── Stage Buckets ──
    Task<IReadOnlyList<StageBucketDefinition>> GetStageBucketsAsync(CancellationToken cancellationToken);
    Task<StageBucketDefinition?> GetStageBucketByIdAsync(int bucketId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StageBucketStage>> GetBucketStagesAsync(int bucketId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StageBucketStage>> GetAllBucketStagesAsync(CancellationToken cancellationToken);
    Task<int> CreateStageBucketAsync(string key, string name, string? appliesToJson, CancellationToken cancellationToken);
    Task<bool> UpdateStageBucketAsync(int bucketId, string name, string? appliesToJson, CancellationToken cancellationToken);
    Task<bool> DeleteStageBucketAsync(int bucketId, CancellationToken cancellationToken);
    Task SetBucketStagesAsync(int bucketId, List<string> stages, CancellationToken cancellationToken);
    Task ReorderBucketsAsync(List<int> bucketIds, CancellationToken cancellationToken);
}
