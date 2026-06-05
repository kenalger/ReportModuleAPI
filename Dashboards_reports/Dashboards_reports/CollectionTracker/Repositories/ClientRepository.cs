using Dashboards_reports.CollectionTracker.Data;
using Dashboards_reports.CollectionTracker.Domain;
using Dashboards_reports.CollectionTracker.Dtos;
using Dapper;
using System.Data;

namespace Dashboards_reports.CollectionTracker.Repositories;

public sealed class ClientRepository(IDbConnectionFactory connectionFactory) : IClientRepository
{
    public async Task<IReadOnlyList<StageDefinition>> GetStagesAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT
                s.Id,
                s.Name,
                s.SortOrder,
                s.IsActive,
                s.CreatedAt,
                s.UpdatedAt
            FROM dbo.StageDefinitions s
            WHERE s.IsActive = 1
            ORDER BY s.SortOrder ASC, s.Id ASC;
            """;

        var stages = await connection.QueryAsync<StageDefinition>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return stages.ToList();
    }

    public async Task<StageDefinition?> GetStageByIdAsync(int stageId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT
                s.Id,
                s.Name,
                s.SortOrder,
                s.IsActive,
                s.CreatedAt,
                s.UpdatedAt
            FROM dbo.StageDefinitions s
            WHERE s.Id = @StageId;
            """;

        return await connection.QueryFirstOrDefaultAsync<StageDefinition>(
            new CommandDefinition(sql, new { StageId = stageId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateStageAsync(string name, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            IF EXISTS
            (
                SELECT 1
                FROM dbo.StageDefinitions
                WHERE IsActive = 1
                  AND Name = @Name
            )
            BEGIN
                SELECT CAST(0 AS INT);
                RETURN;
            END

            DECLARE @NextSortOrder INT;
            SET @NextSortOrder = ISNULL(
                (
                    SELECT MAX(SortOrder) + 1
                    FROM dbo.StageDefinitions
                ),
                0
            );

            INSERT INTO dbo.StageDefinitions
            (
                Name,
                SortOrder,
                IsActive,
                CreatedAt,
                UpdatedAt
            )
            VALUES
            (
                @Name,
                @NextSortOrder,
                1,
                GETDATE(),
                GETDATE()
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Name = name.Trim() }, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateStageAsync(int stageId, string name, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        const string oldStageSql = """
            SELECT s.Name
            FROM dbo.StageDefinitions s
            WHERE s.Id = @StageId
              AND s.IsActive = 1;
            """;

        var oldName = await connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(
                oldStageSql,
                new { StageId = stageId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (string.IsNullOrWhiteSpace(oldName))
        {
            transaction.Rollback();
            return false;
        }

        const string updateStageSql = """
            UPDATE dbo.StageDefinitions
            SET
                Name = @Name,
                UpdatedAt = GETDATE()
            WHERE Id = @StageId
              AND IsActive = 1;
            """;

        var updated = await connection.ExecuteAsync(
            new CommandDefinition(
                updateStageSql,
                new { StageId = stageId, Name = name.Trim() },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (updated == 0)
        {
            transaction.Rollback();
            return false;
        }

        const string updateClientsSql = """
            UPDATE dbo.Clients
            SET
                Stage = @NewStageName,
                UpdatedAt = GETDATE()
            WHERE Stage = @OldStageName;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateClientsSql,
                new
                {
                    OldStageName = oldName,
                    NewStageName = name.Trim()
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        transaction.Commit();
        return true;
    }

    public async Task<bool> DeleteStageAsync(
        int stageId,
        string stageName,
        string? replacementStageName,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        if (!string.IsNullOrWhiteSpace(replacementStageName))
        {
            const string updateClientsSql = """
                UPDATE dbo.Clients
                SET
                    Stage = @ReplacementStageName,
                    UpdatedAt = GETDATE()
                WHERE Stage = @StageName;
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    updateClientsSql,
                    new
                    {
                        StageName = stageName,
                        ReplacementStageName = replacementStageName
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
        }

        const string sql = """
            DELETE FROM dbo.StageDefinitions
            WHERE Id = @StageId
              AND IsActive = 1;
            """;

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { StageId = stageId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (rows == 0)
        {
            transaction.Rollback();
            return false;
        }

        transaction.Commit();
        return true;
    }

    public async Task<bool> IsStageUsedAsync(string stageName, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT TOP (1) 1
            FROM dbo.Clients
            WHERE Stage = @StageName;
            """;

        var inUse = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(sql, new { StageName = stageName }, cancellationToken: cancellationToken));

        return inUse.HasValue;
    }

    public async Task ReorderStagesAsync(List<int> stageIds, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        for (var i = 0; i < stageIds.Count; i++)
        {
            const string sql = """
                UPDATE dbo.StageDefinitions
                SET SortOrder = @SortOrder, UpdatedAt = GETDATE()
                WHERE Id = @StageId AND IsActive = 1;
                """;
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new { StageId = stageIds[i], SortOrder = i }, transaction: transaction, cancellationToken: cancellationToken));
        }

        transaction.Commit();
    }

    public async Task<IReadOnlyList<DelayReasonDefinition>> GetDelayReasonsAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT
                d.Id,
                d.Name,
                d.SortOrder,
                d.IsActive,
                d.CreatedAt,
                d.UpdatedAt
            FROM dbo.DelayReasonDefinitions d
            WHERE d.IsActive = 1
            ORDER BY d.SortOrder ASC, d.Id ASC;
            """;

        var reasons = await connection.QueryAsync<DelayReasonDefinition>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return reasons.ToList();
    }

    public async Task<DelayReasonDefinition?> GetDelayReasonByIdAsync(int delayReasonId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT
                d.Id,
                d.Name,
                d.SortOrder,
                d.IsActive,
                d.CreatedAt,
                d.UpdatedAt
            FROM dbo.DelayReasonDefinitions d
            WHERE d.Id = @DelayReasonId;
            """;

        return await connection.QueryFirstOrDefaultAsync<DelayReasonDefinition>(
            new CommandDefinition(sql, new { DelayReasonId = delayReasonId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateDelayReasonAsync(string name, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            IF EXISTS
            (
                SELECT 1
                FROM dbo.DelayReasonDefinitions
                WHERE IsActive = 1
                  AND Name = @Name
            )
            BEGIN
                SELECT CAST(0 AS INT);
                RETURN;
            END

            DECLARE @NextSortOrder INT;
            SET @NextSortOrder = ISNULL(
                (
                    SELECT MAX(SortOrder) + 1
                    FROM dbo.DelayReasonDefinitions
                ),
                0
            );

            INSERT INTO dbo.DelayReasonDefinitions
            (
                Name,
                SortOrder,
                IsActive,
                CreatedAt,
                UpdatedAt
            )
            VALUES
            (
                @Name,
                @NextSortOrder,
                1,
                GETDATE(),
                GETDATE()
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Name = name.Trim() }, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateDelayReasonAsync(int delayReasonId, string name, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        const string oldReasonSql = """
            SELECT d.Name
            FROM dbo.DelayReasonDefinitions d
            WHERE d.Id = @DelayReasonId
              AND d.IsActive = 1;
            """;

        var oldName = await connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(
                oldReasonSql,
                new { DelayReasonId = delayReasonId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (string.IsNullOrWhiteSpace(oldName))
        {
            transaction.Rollback();
            return false;
        }

        const string updateReasonSql = """
            UPDATE dbo.DelayReasonDefinitions
            SET
                Name = @Name,
                UpdatedAt = GETDATE()
            WHERE Id = @DelayReasonId
              AND IsActive = 1;
            """;

        var updated = await connection.ExecuteAsync(
            new CommandDefinition(
                updateReasonSql,
                new { DelayReasonId = delayReasonId, Name = name.Trim() },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (updated == 0)
        {
            transaction.Rollback();
            return false;
        }

        const string updateClientsSql = """
            UPDATE dbo.Clients
            SET
                DelayReason = CASE WHEN DelayReason = @OldReasonName THEN @NewReasonName ELSE DelayReason END,
                SecondaryDelayReason = CASE WHEN SecondaryDelayReason = @OldReasonName THEN @NewReasonName ELSE SecondaryDelayReason END,
                UpdatedAt = GETDATE()
            WHERE DelayReason = @OldReasonName
               OR SecondaryDelayReason = @OldReasonName;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateClientsSql,
                new
                {
                    OldReasonName = oldName,
                    NewReasonName = name.Trim()
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        const string updateLinkSql = """
            UPDATE dbo.ClientDelayReasons
            SET DelayReason = @NewReasonName
            WHERE DelayReason = @OldReasonName;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateLinkSql,
                new
                {
                    OldReasonName = oldName,
                    NewReasonName = name.Trim()
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        transaction.Commit();
        return true;
    }

    public async Task<bool> DeleteDelayReasonAsync(
        int delayReasonId,
        string delayReasonName,
        string replacementDelayReasonName,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string updateClientsSql = """
            UPDATE dbo.Clients
            SET
                DelayReason = CASE WHEN DelayReason = @DelayReasonName THEN @ReplacementDelayReasonName ELSE DelayReason END,
                SecondaryDelayReason =
                    CASE
                        WHEN SecondaryDelayReason = @DelayReasonName AND @ReplacementDelayReasonName = 'None' THEN NULL
                        WHEN SecondaryDelayReason = @DelayReasonName THEN @ReplacementDelayReasonName
                        ELSE SecondaryDelayReason
                    END,
                UpdatedAt = GETDATE()
            WHERE DelayReason = @DelayReasonName
               OR SecondaryDelayReason = @DelayReasonName;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateClientsSql,
                new
                {
                    DelayReasonName = delayReasonName,
                    ReplacementDelayReasonName = replacementDelayReasonName
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        const string updateLinksSql = """
            UPDATE dbo.ClientDelayReasons
            SET DelayReason = @ReplacementDelayReasonName
            WHERE DelayReason = @DelayReasonName;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateLinksSql,
                new
                {
                    DelayReasonName = delayReasonName,
                    ReplacementDelayReasonName = replacementDelayReasonName
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        const string removeNoneLinksSql = """
            DELETE FROM dbo.ClientDelayReasons
            WHERE DelayReason = N'None';
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                removeNoneLinksSql,
                transaction: transaction,
                cancellationToken: cancellationToken));

        const string deleteSql = """
            DELETE FROM dbo.DelayReasonDefinitions
            WHERE Id = @DelayReasonId
              AND IsActive = 1;
            """;

        var deleted = await connection.ExecuteAsync(
            new CommandDefinition(
                deleteSql,
                new { DelayReasonId = delayReasonId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (deleted == 0)
        {
            transaction.Rollback();
            return false;
        }

        transaction.Commit();
        return true;
    }

    public async Task<bool> IsDelayReasonUsedAsync(string delayReasonName, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT TOP (1) 1
            FROM dbo.Clients
            WHERE DelayReason = @DelayReasonName
               OR SecondaryDelayReason = @DelayReasonName;
            """;

        var inUse = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(sql, new { DelayReasonName = delayReasonName }, cancellationToken: cancellationToken));

        return inUse.HasValue;
    }

    public async Task<IReadOnlyList<FinancingTypeDefinition>> GetFinancingTypesAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT
                f.Id,
                f.Name,
                f.SortOrder,
                f.IsActive,
                f.CreatedAt,
                f.UpdatedAt
            FROM dbo.FinancingTypeDefinitions f
            WHERE f.IsActive = 1
            ORDER BY f.SortOrder ASC, f.Id ASC;
            """;

        var rows = await connection.QueryAsync<FinancingTypeDefinition>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<FinancingTypeDefinition?> GetFinancingTypeByIdAsync(int financingTypeId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT
                f.Id,
                f.Name,
                f.SortOrder,
                f.IsActive,
                f.CreatedAt,
                f.UpdatedAt
            FROM dbo.FinancingTypeDefinitions f
            WHERE f.Id = @FinancingTypeId;
            """;

        return await connection.QueryFirstOrDefaultAsync<FinancingTypeDefinition>(
            new CommandDefinition(sql, new { FinancingTypeId = financingTypeId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateFinancingTypeAsync(string name, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            IF EXISTS
            (
                SELECT 1
                FROM dbo.FinancingTypeDefinitions
                WHERE IsActive = 1
                  AND Name = @Name
            )
            BEGIN
                SELECT CAST(0 AS INT);
                RETURN;
            END

            DECLARE @NextSortOrder INT;
            SET @NextSortOrder = ISNULL(
                (
                    SELECT MAX(SortOrder) + 1
                    FROM dbo.FinancingTypeDefinitions
                ),
                0
            );

            INSERT INTO dbo.FinancingTypeDefinitions
            (
                Name,
                SortOrder,
                IsActive,
                CreatedAt,
                UpdatedAt
            )
            VALUES
            (
                @Name,
                @NextSortOrder,
                1,
                GETDATE(),
                GETDATE()
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Name = name.Trim() }, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateFinancingTypeAsync(int financingTypeId, string name, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string currentNameSql = """
            SELECT f.Name
            FROM dbo.FinancingTypeDefinitions f
            WHERE f.Id = @FinancingTypeId
              AND f.IsActive = 1;
            """;

        var oldName = await connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(
                currentNameSql,
                new { FinancingTypeId = financingTypeId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (string.IsNullOrWhiteSpace(oldName))
        {
            transaction.Rollback();
            return false;
        }

        const string updateDefinitionSql = """
            UPDATE dbo.FinancingTypeDefinitions
            SET
                Name = @Name,
                UpdatedAt = GETDATE()
            WHERE Id = @FinancingTypeId
              AND IsActive = 1;
            """;

        var updated = await connection.ExecuteAsync(
            new CommandDefinition(
                updateDefinitionSql,
                new { FinancingTypeId = financingTypeId, Name = name.Trim() },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (updated == 0)
        {
            transaction.Rollback();
            return false;
        }

        const string updateClientsSql = """
            UPDATE dbo.Clients
            SET
                FinancingType = @NewFinancingTypeName,
                UpdatedAt = GETDATE()
            WHERE FinancingType = @OldFinancingTypeName;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateClientsSql,
                new
                {
                    OldFinancingTypeName = oldName,
                    NewFinancingTypeName = name.Trim()
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        transaction.Commit();
        return true;
    }

    public async Task<bool> DeleteFinancingTypeAsync(
        int financingTypeId,
        string financingTypeName,
        string replacementFinancingTypeName,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string updateClientsSql = """
            UPDATE dbo.Clients
            SET
                FinancingType = @ReplacementFinancingTypeName,
                UpdatedAt = GETDATE()
            WHERE FinancingType = @FinancingTypeName;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateClientsSql,
                new
                {
                    FinancingTypeName = financingTypeName,
                    ReplacementFinancingTypeName = replacementFinancingTypeName
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        const string deleteSql = """
            DELETE FROM dbo.FinancingTypeDefinitions
            WHERE Id = @FinancingTypeId
              AND IsActive = 1;
            """;

        var deleted = await connection.ExecuteAsync(
            new CommandDefinition(
                deleteSql,
                new { FinancingTypeId = financingTypeId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (deleted == 0)
        {
            transaction.Rollback();
            return false;
        }

        transaction.Commit();
        return true;
    }

    public async Task<bool> IsFinancingTypeUsedAsync(string financingTypeName, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT TOP (1) 1
            FROM dbo.Clients
            WHERE FinancingType = @FinancingTypeName;
            """;

        var inUse = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(sql, new { FinancingTypeName = financingTypeName }, cancellationToken: cancellationToken));

        return inUse.HasValue;
    }

    public async Task<IReadOnlyList<ActivityTypeDefinition>> GetActivityTypesAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT
                a.Id,
                a.Code,
                a.Label,
                a.SortOrder,
                a.IsActive,
                a.CreatedAt,
                a.UpdatedAt
            FROM dbo.ActivityTypeDefinitions a
            WHERE a.IsActive = 1
            ORDER BY a.SortOrder ASC, a.Id ASC;
            """;

        var rows = await connection.QueryAsync<ActivityTypeDefinition>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<ActivityTypeDefinition?> GetActivityTypeByIdAsync(int activityTypeId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT
                a.Id,
                a.Code,
                a.Label,
                a.SortOrder,
                a.IsActive,
                a.CreatedAt,
                a.UpdatedAt
            FROM dbo.ActivityTypeDefinitions a
            WHERE a.Id = @ActivityTypeId;
            """;

        return await connection.QueryFirstOrDefaultAsync<ActivityTypeDefinition>(
            new CommandDefinition(sql, new { ActivityTypeId = activityTypeId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateActivityTypeAsync(string code, string label, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            IF EXISTS
            (
                SELECT 1
                FROM dbo.ActivityTypeDefinitions
                WHERE IsActive = 1
                  AND Code = @Code
            )
            BEGIN
                SELECT CAST(0 AS INT);
                RETURN;
            END

            DECLARE @NextSortOrder INT;
            SET @NextSortOrder = ISNULL(
                (
                    SELECT MAX(SortOrder) + 1
                    FROM dbo.ActivityTypeDefinitions
                ),
                0
            );

            INSERT INTO dbo.ActivityTypeDefinitions
            (
                Code,
                Label,
                SortOrder,
                IsActive,
                CreatedAt,
                UpdatedAt
            )
            VALUES
            (
                @Code,
                @Label,
                @NextSortOrder,
                1,
                GETDATE(),
                GETDATE()
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Code = code.Trim().ToLowerInvariant(), Label = label.Trim() }, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateActivityTypeAsync(int activityTypeId, string code, string label, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string currentCodeSql = """
            SELECT a.Code
            FROM dbo.ActivityTypeDefinitions a
            WHERE a.Id = @ActivityTypeId
              AND a.IsActive = 1;
            """;

        var oldCode = await connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(
                currentCodeSql,
                new { ActivityTypeId = activityTypeId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (string.IsNullOrWhiteSpace(oldCode))
        {
            transaction.Rollback();
            return false;
        }

        const string updateDefinitionSql = """
            UPDATE dbo.ActivityTypeDefinitions
            SET
                Code = @Code,
                Label = @Label,
                UpdatedAt = GETDATE()
            WHERE Id = @ActivityTypeId
              AND IsActive = 1;
            """;

        var updated = await connection.ExecuteAsync(
            new CommandDefinition(
                updateDefinitionSql,
                new
                {
                    ActivityTypeId = activityTypeId,
                    Code = code.Trim().ToLowerInvariant(),
                    Label = label.Trim()
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (updated == 0)
        {
            transaction.Rollback();
            return false;
        }

        const string updateLogsSql = """
            UPDATE dbo.ActivityLogs
            SET ActivityType = @NewCode
            WHERE ActivityType = @OldCode;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateLogsSql,
                new
                {
                    OldCode = oldCode,
                    NewCode = code.Trim().ToLowerInvariant()
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        transaction.Commit();
        return true;
    }

    public async Task<bool> DeleteActivityTypeAsync(
        int activityTypeId,
        string activityTypeCode,
        string replacementActivityTypeCode,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string updateLogsSql = """
            UPDATE dbo.ActivityLogs
            SET ActivityType = @ReplacementCode
            WHERE ActivityType = @Code;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateLogsSql,
                new
                {
                    Code = activityTypeCode,
                    ReplacementCode = replacementActivityTypeCode
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        const string deleteSql = """
            DELETE FROM dbo.ActivityTypeDefinitions
            WHERE Id = @ActivityTypeId
              AND IsActive = 1;
            """;

        var deleted = await connection.ExecuteAsync(
            new CommandDefinition(
                deleteSql,
                new { ActivityTypeId = activityTypeId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (deleted == 0)
        {
            transaction.Rollback();
            return false;
        }

        transaction.Commit();
        return true;
    }

    public async Task<bool> IsActivityTypeUsedAsync(string activityTypeCode, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT TOP (1) 1
            FROM dbo.ActivityLogs
            WHERE ActivityType = @Code;
            """;

        var inUse = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(sql, new { Code = activityTypeCode }, cancellationToken: cancellationToken));

        return inUse.HasValue;
    }

    public async Task<IReadOnlyList<Client>> GetClientsAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        const string sql = """
            SELECT
                c.Id,
                c.Name,
                c.UnitId,
                ISNULL(pu.Name, c.Unit) AS Unit,
                pu.ProjectId,
                p.Name AS ProjectName,
                pu.TotalContractPrice,
                c.ContactNumber,
                c.BrokerName,
                c.FinancingType,
                c.Stage,
                c.StageDate,
                c.TargetDate,
                c.ResolvedDate,
                c.DelayReason,
                c.SecondaryDelayReason,
                c.NextAction,
                c.FollowUpDate,
                c.Notes,
                c.AddedDate,
                c.ResolvedHow,
                c.ResolvedNotes,
                c.CreatedBy,
                c.ModifiedBy,
                c.CreatedAt,
                c.UpdatedAt
            FROM dbo.Clients c
            LEFT JOIN dbo.ProjectUnits pu ON pu.Id = c.UnitId
            LEFT JOIN dbo.Projects p ON p.Id = pu.ProjectId;

            SELECT
                a.Id,
                a.ClientId,
                a.ActivityType,
                a.Description,
                a.ActivityDateTime,
                a.CreatedBy,
                a.CreatedAt
            FROM dbo.ActivityLogs a;

            SELECT
                t.Id,
                t.ClientId,
                t.Description,
                t.DueDate,
                t.Priority,
                t.AssignedTo,
                t.IsDone,
                t.DoneAt,
                t.AddedAt
            FROM dbo.TaskItems t;

            SELECT
                cdr.ClientId,
                cdr.DelayReason,
                cdr.SortOrder
            FROM dbo.ClientDelayReasons cdr
            ORDER BY cdr.ClientId, cdr.SortOrder, cdr.Id;
            """;

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        var clients = (await multi.ReadAsync<Client>()).ToList();
        var activities = await multi.ReadAsync<ActivityLog>();
        var tasks = await multi.ReadAsync<TaskItem>();
        var delayReasonLinks = await multi.ReadAsync<ClientDelayReasonRow>();

        var byId = clients.ToDictionary(c => c.Id);

        foreach (var activity in activities)
        {
            if (byId.TryGetValue(activity.ClientId, out var client))
            {
                client.Activities.Add(activity);
            }
        }

        foreach (var task in tasks)
        {
            if (byId.TryGetValue(task.ClientId, out var client))
            {
                client.Tasks.Add(task);
            }
        }

        foreach (var link in delayReasonLinks)
        {
            if (byId.TryGetValue(link.ClientId, out var client))
            {
                client.DelayReasons.Add(link.DelayReason);
            }
        }

        return clients;
    }

    public async Task<Client?> GetClientByIdAsync(int clientId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        const string sql = """
            SELECT
                c.Id,
                c.Name,
                c.UnitId,
                ISNULL(pu.Name, c.Unit) AS Unit,
                pu.ProjectId,
                p.Name AS ProjectName,
                pu.TotalContractPrice,
                c.ContactNumber,
                c.BrokerName,
                c.FinancingType,
                c.Stage,
                c.StageDate,
                c.TargetDate,
                c.ResolvedDate,
                c.DelayReason,
                c.SecondaryDelayReason,
                c.NextAction,
                c.FollowUpDate,
                c.Notes,
                c.AddedDate,
                c.ResolvedHow,
                c.ResolvedNotes,
                c.CreatedAt,
                c.UpdatedAt
            FROM dbo.Clients c
            LEFT JOIN dbo.ProjectUnits pu ON pu.Id = c.UnitId
            LEFT JOIN dbo.Projects p ON p.Id = pu.ProjectId
            WHERE c.Id = @ClientId;

            SELECT
                a.Id,
                a.ClientId,
                a.ActivityType,
                a.Description,
                a.ActivityDateTime,
                a.CreatedBy,
                a.CreatedAt
            FROM dbo.ActivityLogs a
            WHERE a.ClientId = @ClientId;

            SELECT
                t.Id,
                t.ClientId,
                t.Description,
                t.DueDate,
                t.Priority,
                t.AssignedTo,
                t.IsDone,
                t.DoneAt,
                t.AddedAt
            FROM dbo.TaskItems t
            WHERE t.ClientId = @ClientId;

            SELECT
                cdr.ClientId,
                cdr.DelayReason,
                cdr.SortOrder
            FROM dbo.ClientDelayReasons cdr
            WHERE cdr.ClientId = @ClientId
            ORDER BY cdr.SortOrder, cdr.Id;
            """;

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { ClientId = clientId }, cancellationToken: cancellationToken));

        var client = await multi.ReadFirstOrDefaultAsync<Client>();
        if (client is null)
        {
            return null;
        }

        var activities = await multi.ReadAsync<ActivityLog>();
        var tasks = await multi.ReadAsync<TaskItem>();
        var delayReasonLinks = await multi.ReadAsync<ClientDelayReasonRow>();

        client.Activities.AddRange(activities);
        client.Tasks.AddRange(tasks);
        client.DelayReasons.AddRange(delayReasonLinks.Select(x => x.DelayReason));

        return client;
    }

    public async Task<int> CreateClientAsync(UpsertClientRequest request, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        const string sql = """
            INSERT INTO dbo.Clients
            (
                Name,
                UnitId,
                Unit,
                TotalContractPrice,
                ContactNumber,
                BrokerName,
                FinancingType,
                Stage,
                StageDate,
                TargetDate,
                ResolvedDate,
                DelayReason,
                SecondaryDelayReason,
                NextAction,
                FollowUpDate,
                Notes,
                AddedDate,
                ResolvedHow,
                ResolvedNotes,
                CreatedBy,
                ModifiedBy,
                CreatedAt,
                UpdatedAt
            )
            VALUES
            (
                @Name,
                @UnitId,
                @Unit,
                @TotalContractPrice,
                @ContactNumber,
                @BrokerName,
                @FinancingType,
                @Stage,
                @StageDate,
                @TargetDate,
                @ResolvedDate,
                @DelayReason,
                @SecondaryDelayReason,
                @NextAction,
                @FollowUpDate,
                @Notes,
                CAST(GETDATE() AS DATE),
                NULL,
                NULL,
                @CreatedBy,
                @ModifiedBy,
                GETDATE(),
                GETDATE()
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        var clientId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, request, transaction: transaction, cancellationToken: cancellationToken));

        await SyncClientDelayReasonsAsync(
            connection,
            transaction,
            clientId,
            request.DelayReasons,
            request.DelayReason,
            request.SecondaryDelayReason,
            cancellationToken);

        transaction.Commit();
        return clientId;
    }

    public async Task<bool> UpdateClientAsync(int clientId, UpsertClientRequest request, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        // Detect stage change — read old stage before updating
        const string oldStageSql = """
            SELECT Stage FROM dbo.Clients WHERE Id = @ClientId;
            """;
        var oldStage = await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(oldStageSql, new { ClientId = clientId }, transaction: transaction, cancellationToken: cancellationToken));

        var stageChanged = oldStage is not null &&
            !string.Equals(oldStage, request.Stage, StringComparison.OrdinalIgnoreCase);

        // If stage changed, override StageDate to today
        var stageDate = stageChanged ? DateTime.UtcNow.Date : request.StageDate;

        const string sql = """
            UPDATE dbo.Clients
            SET
                Name = @Name,
                UnitId = @UnitId,
                Unit = @Unit,
                TotalContractPrice = @TotalContractPrice,
                ContactNumber = @ContactNumber,
                BrokerName = @BrokerName,
                FinancingType = @FinancingType,
                Stage = @Stage,
                StageDate = @StageDate,
                TargetDate = @TargetDate,
                ResolvedDate = @ResolvedDate,
                DelayReason = @DelayReason,
                SecondaryDelayReason = @SecondaryDelayReason,
                NextAction = @NextAction,
                FollowUpDate = @FollowUpDate,
                Notes = @Notes,
                ModifiedBy = @ModifiedBy,
                UpdatedAt = GETDATE()
            WHERE Id = @ClientId;
            """;

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    ClientId = clientId,
                    request.Name,
                    request.UnitId,
                    request.Unit,
                    request.TotalContractPrice,
                    request.ContactNumber,
                    request.BrokerName,
                    request.FinancingType,
                    request.Stage,
                    StageDate = stageDate,
                    request.TargetDate,
                    request.ResolvedDate,
                    request.DelayReason,
                    request.SecondaryDelayReason,
                    request.NextAction,
                    request.FollowUpDate,
                    request.Notes,
                    request.ModifiedBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
        if (rows == 0)
        {
            transaction.Rollback();
            return false;
        }

        // Auto-log stage change as system activity
        if (stageChanged)
        {
            const string logSql = """
                INSERT INTO dbo.ActivityLogs (ClientId, ActivityType, Description, ActivityDateTime, CreatedBy, CreatedAt)
                VALUES (@ClientId, 'system', @Description, GETDATE(), @CreatedBy, GETDATE());
                """;
            await connection.ExecuteAsync(
                new CommandDefinition(
                    logSql,
                    new
                    {
                        ClientId = clientId,
                        Description = $"Stage changed: {oldStage} → {request.Stage}",
                        CreatedBy = request.ModifiedBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
        }

        await SyncClientDelayReasonsAsync(
            connection,
            transaction,
            clientId,
            request.DelayReasons,
            request.DelayReason,
            request.SecondaryDelayReason,
            cancellationToken);

        transaction.Commit();
        return true;
    }

    public async Task<bool> DeleteClientAsync(int clientId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            DELETE FROM dbo.Clients
            WHERE Id = @ClientId;
            """;

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { ClientId = clientId }, cancellationToken: cancellationToken));

        return rows > 0;
    }

    public async Task<bool> ResolveClientAsync(int clientId, ResolveClientRequest request, string? modifiedBy, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var resolvedDate = request.ResolvedDate?.Date ?? DateTime.UtcNow.Date;
        var notesPart = string.IsNullOrWhiteSpace(request.ResolvedNotes)
            ? request.ResolvedHow.Trim()
            : $"{request.ResolvedHow.Trim()} - {request.ResolvedNotes.Trim()}";
        var stamp = $"[RESOLVED {resolvedDate:yyyy-MM-dd}] {notesPart}";

        const string updateSql = """
            UPDATE dbo.Clients
            SET
                Stage = 'Resolved',
                ResolvedDate = @ResolvedDate,
                ResolvedHow = @ResolvedHow,
                ResolvedNotes = @ResolvedNotes,
                UnitId = CASE WHEN @ResolvedHow = 'Account Cancelled' THEN NULL ELSE UnitId END,
                DelayReason = CASE WHEN @ResolvedHow = 'Account Cancelled' THEN DelayReason ELSE 'None' END,
                SecondaryDelayReason = CASE WHEN @ResolvedHow = 'Account Cancelled' THEN SecondaryDelayReason ELSE NULL END,
                Notes =
                    CASE
                        WHEN @Stamp IS NULL OR LTRIM(RTRIM(@Stamp)) = '' THEN Notes
                        WHEN Notes IS NULL OR LTRIM(RTRIM(Notes)) = '' THEN @Stamp
                        ELSE Notes + CHAR(13) + CHAR(10) + @Stamp
                    END,
                ModifiedBy = @ModifiedBy,
                UpdatedAt = GETDATE()
            WHERE Id = @ClientId;
            """;

        var updated = await connection.ExecuteAsync(
            new CommandDefinition(
                updateSql,
                new
                {
                    ClientId = clientId,
                    ResolvedDate = resolvedDate,
                    ResolvedHow = request.ResolvedHow.Trim(),
                    ResolvedNotes = string.IsNullOrWhiteSpace(request.ResolvedNotes) ? null : request.ResolvedNotes.Trim(),
                    Stamp = stamp,
                    ModifiedBy = modifiedBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (updated == 0)
        {
            transaction.Rollback();
            return false;
        }

        const string insertActivitySql = """
            INSERT INTO dbo.ActivityLogs
            (
                ClientId,
                ActivityType,
                Description,
                ActivityDateTime,
                CreatedBy,
                CreatedAt
            )
            VALUES
            (
                @ClientId,
                'system',
                @Description,
                GETDATE(),
                @CreatedBy,
                GETDATE()
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertActivitySql,
                new
                {
                    ClientId = clientId,
                    Description = $"Account resolved: {notesPart}",
                    CreatedBy = modifiedBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        transaction.Commit();
        return true;
    }

    public async Task<int> AddActivityAsync(int clientId, AddActivityRequest request, string? createdBy, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Clients WHERE Id = @ClientId)
            BEGIN
                SELECT CAST(0 AS INT);
                RETURN;
            END

            INSERT INTO dbo.ActivityLogs
            (
                ClientId,
                ActivityType,
                Description,
                ActivityDateTime,
                CreatedBy,
                DelayReason,
                CreatedAt
            )
            VALUES
            (
                @ClientId,
                @ActivityType,
                @Description,
                @ActivityDateTime,
                @CreatedBy,
                @DelayReason,
                GETDATE()
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        var activityDateTime = request.ActivityDateTime ?? DateTime.UtcNow;
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    ClientId = clientId,
                    ActivityType = request.ActivityType.Trim().ToLowerInvariant(),
                    Description = request.Description.Trim(),
                    ActivityDateTime = activityDateTime,
                    CreatedBy = createdBy,
                    DelayReason = string.IsNullOrWhiteSpace(request.DelayReason) ? null : request.DelayReason.Trim()
                },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteActivityAsync(int clientId, int activityId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            DELETE FROM dbo.ActivityLogs
            WHERE Id = @ActivityId
              AND ClientId = @ClientId;
            """;

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { ClientId = clientId, ActivityId = activityId },
                cancellationToken: cancellationToken));

        return rows > 0;
    }

    public async Task<int> AddTaskAsync(int clientId, AddTaskRequest request, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Clients WHERE Id = @ClientId)
            BEGIN
                SELECT CAST(0 AS INT);
                RETURN;
            END

            INSERT INTO dbo.TaskItems
            (
                ClientId,
                Description,
                DueDate,
                Priority,
                AssignedTo,
                IsDone,
                DoneAt,
                AddedAt
            )
            VALUES
            (
                @ClientId,
                @Description,
                @DueDate,
                @Priority,
                @AssignedTo,
                0,
                NULL,
                GETDATE()
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        var taskId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    ClientId = clientId,
                    Description = request.Description.Trim(),
                    DueDate = request.DueDate?.Date,
                    Priority = request.Priority.Trim().ToLowerInvariant(),
                    AssignedTo = string.IsNullOrWhiteSpace(request.AssignedTo) ? null : request.AssignedTo.Trim()
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (taskId == 0)
        {
            transaction.Rollback();
            return 0;
        }

        await SyncNextActionAsync(connection, transaction, clientId, cancellationToken);
        transaction.Commit();
        return taskId;
    }

    public async Task<bool> UpdateTaskStatusAsync(int clientId, int taskId, bool isDone, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string sql = """
            UPDATE dbo.TaskItems
            SET
                IsDone = @IsDone,
                DoneAt = CASE WHEN @IsDone = 1 THEN GETDATE() ELSE NULL END
            WHERE Id = @TaskId
              AND ClientId = @ClientId;
            """;

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { ClientId = clientId, TaskId = taskId, IsDone = isDone },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (rows == 0)
        {
            transaction.Rollback();
            return false;
        }

        await SyncNextActionAsync(connection, transaction, clientId, cancellationToken);
        transaction.Commit();
        return true;
    }

    public async Task<bool> DeleteTaskAsync(int clientId, int taskId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string deleteSql = """
            DELETE FROM dbo.TaskItems
            WHERE Id = @TaskId
              AND ClientId = @ClientId;
            """;

        var deleted = await connection.ExecuteAsync(
            new CommandDefinition(
                deleteSql,
                new { ClientId = clientId, TaskId = taskId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (deleted == 0)
        {
            transaction.Rollback();
            return false;
        }

        await SyncNextActionAsync(connection, transaction, clientId, cancellationToken);
        transaction.Commit();
        return true;
    }

    private static async Task SyncClientDelayReasonsAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        int clientId,
        IReadOnlyList<string>? delayReasons,
        string? legacyDelayReason,
        string? legacySecondaryDelayReason,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeDelayReasons(delayReasons, legacyDelayReason, legacySecondaryDelayReason);

        const string deleteSql = """
            DELETE FROM dbo.ClientDelayReasons
            WHERE ClientId = @ClientId;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                deleteSql,
                new { ClientId = clientId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (normalized.Count == 0)
        {
            return;
        }

        const string insertSql = """
            INSERT INTO dbo.ClientDelayReasons
            (
                ClientId,
                DelayReason,
                SortOrder
            )
            VALUES
            (
                @ClientId,
                @DelayReason,
                @SortOrder
            );
            """;

        for (var i = 0; i < normalized.Count; i++)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertSql,
                    new
                    {
                        ClientId = clientId,
                        DelayReason = normalized[i],
                        SortOrder = i
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
        }
    }

    private static List<string> NormalizeDelayReasons(
        IReadOnlyList<string>? delayReasons,
        string? legacyDelayReason,
        string? legacySecondaryDelayReason)
    {
        var fromList = (delayReasons ?? [])
            .Where(reason => !string.IsNullOrWhiteSpace(reason))
            .Select(reason => reason.Trim())
            .Where(reason => !string.Equals(reason, "None", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (fromList.Count > 0)
        {
            return fromList;
        }

        return new[] { legacyDelayReason, legacySecondaryDelayReason }
            .Where(reason => !string.IsNullOrWhiteSpace(reason))
            .Select(reason => reason!.Trim())
            .Where(reason => !string.Equals(reason, "None", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task EnsureStagesSchemaAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            -- ── Projects table (must exist before Clients/ProjectUnits references) ──
            IF OBJECT_ID(N'dbo.Projects', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Projects
                (
                    Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Name      NVARCHAR(200) NOT NULL,
                    SortOrder INT NOT NULL CONSTRAINT DF_Projects_SortOrder DEFAULT (0),
                    IsActive  BIT NOT NULL CONSTRAINT DF_Projects_IsActive DEFAULT (1),
                    CreatedAt DATETIME NOT NULL CONSTRAINT DF_Projects_CreatedAt DEFAULT (GETDATE()),
                    UpdatedAt DATETIME NOT NULL CONSTRAINT DF_Projects_UpdatedAt DEFAULT (GETDATE())
                );

                CREATE UNIQUE INDEX UX_Projects_Name
                    ON dbo.Projects(Name) WHERE IsActive = 1;
            END;

            -- ── ProjectUnits table ──
            IF OBJECT_ID(N'dbo.ProjectUnits', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ProjectUnits
                (
                    Id                 INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    ProjectId          INT NOT NULL,
                    Name               NVARCHAR(200) NOT NULL,
                    TotalContractPrice DECIMAL(18,2) NULL,
                    SortOrder          INT NOT NULL CONSTRAINT DF_ProjectUnits_SortOrder DEFAULT (0),
                    IsActive           BIT NOT NULL CONSTRAINT DF_ProjectUnits_IsActive DEFAULT (1),
                    CreatedAt          DATETIME NOT NULL CONSTRAINT DF_ProjectUnits_CreatedAt DEFAULT (GETDATE()),
                    UpdatedAt          DATETIME NOT NULL CONSTRAINT DF_ProjectUnits_UpdatedAt DEFAULT (GETDATE()),

                    CONSTRAINT FK_ProjectUnits_Projects FOREIGN KEY (ProjectId)
                        REFERENCES dbo.Projects(Id)
                );

                CREATE INDEX IX_ProjectUnits_ProjectId
                    ON dbo.ProjectUnits(ProjectId);
            END;

            IF OBJECT_ID(N'dbo.Clients', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Clients
                (
                    Id                   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Name                 NVARCHAR(200) NOT NULL,
                    UnitId               INT NULL,
                    Unit                 NVARCHAR(200) NOT NULL,
                    ContactNumber        NVARCHAR(50) NULL,
                    BrokerName           NVARCHAR(200) NULL,
                    TotalContractPrice   DECIMAL(18,2) NULL,
                    FinancingType        NVARCHAR(60) NOT NULL CONSTRAINT DF_Clients_FinancingType DEFAULT (N'Bank'),
                    Stage                NVARCHAR(80) NOT NULL CONSTRAINT DF_Clients_Stage DEFAULT (N'Reservation'),
                    StageDate            DATETIME NULL,
                    TargetDate           DATETIME NULL,
                    ResolvedDate         DATETIME NULL,
                    DelayReason          NVARCHAR(120) NOT NULL CONSTRAINT DF_Clients_DelayReason DEFAULT (N'None'),
                    SecondaryDelayReason NVARCHAR(120) NULL,
                    NextAction           NVARCHAR(500) NULL,
                    FollowUpDate         DATETIME NULL,
                    Notes                NVARCHAR(MAX) NULL,
                    AddedDate            DATETIME NOT NULL CONSTRAINT DF_Clients_AddedDate DEFAULT (GETDATE()),
                    ResolvedHow          NVARCHAR(200) NULL,
                    ResolvedNotes        NVARCHAR(MAX) NULL,
                    CreatedBy            NVARCHAR(100) NULL,
                    ModifiedBy           NVARCHAR(100) NULL,
                    CreatedAt            DATETIME NOT NULL CONSTRAINT DF_Clients_CreatedAt DEFAULT (GETDATE()),
                    UpdatedAt            DATETIME NOT NULL CONSTRAINT DF_Clients_UpdatedAt DEFAULT (GETDATE())
                );
            END;

            -- Migrate existing Clients table: add any missing columns
            IF OBJECT_ID(N'dbo.Clients', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'UnitId')
                    ALTER TABLE dbo.Clients ADD UnitId INT NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'TotalContractPrice')
                    ALTER TABLE dbo.Clients ADD TotalContractPrice DECIMAL(18,2) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'Unit')
                    ALTER TABLE dbo.Clients ADD Unit NVARCHAR(200) NOT NULL CONSTRAINT DF_Clients_Unit_Mig DEFAULT (N'');
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'ContactNumber')
                    ALTER TABLE dbo.Clients ADD ContactNumber NVARCHAR(50) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'BrokerName')
                    ALTER TABLE dbo.Clients ADD BrokerName NVARCHAR(200) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'FinancingType')
                    ALTER TABLE dbo.Clients ADD FinancingType NVARCHAR(60) NOT NULL CONSTRAINT DF_Clients_FinancingType_Mig DEFAULT (N'Bank');
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'Stage')
                    ALTER TABLE dbo.Clients ADD Stage NVARCHAR(80) NOT NULL CONSTRAINT DF_Clients_Stage_Mig DEFAULT (N'Reservation');
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'StageDate')
                    ALTER TABLE dbo.Clients ADD StageDate DATETIME NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'TargetDate')
                    ALTER TABLE dbo.Clients ADD TargetDate DATETIME NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'ResolvedDate')
                    ALTER TABLE dbo.Clients ADD ResolvedDate DATETIME NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'DelayReason')
                    ALTER TABLE dbo.Clients ADD DelayReason NVARCHAR(120) NOT NULL CONSTRAINT DF_Clients_DelayReason_Mig DEFAULT (N'None');
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'SecondaryDelayReason')
                    ALTER TABLE dbo.Clients ADD SecondaryDelayReason NVARCHAR(120) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'NextAction')
                    ALTER TABLE dbo.Clients ADD NextAction NVARCHAR(500) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'FollowUpDate')
                    ALTER TABLE dbo.Clients ADD FollowUpDate DATETIME NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'Notes')
                    ALTER TABLE dbo.Clients ADD Notes NVARCHAR(MAX) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'AddedDate')
                    ALTER TABLE dbo.Clients ADD AddedDate DATETIME NOT NULL CONSTRAINT DF_Clients_AddedDate_Mig DEFAULT (GETDATE());
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'ResolvedHow')
                    ALTER TABLE dbo.Clients ADD ResolvedHow NVARCHAR(200) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'ResolvedNotes')
                    ALTER TABLE dbo.Clients ADD ResolvedNotes NVARCHAR(MAX) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'CreatedAt')
                    ALTER TABLE dbo.Clients ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_Clients_CreatedAt_Mig DEFAULT (GETDATE());
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'UpdatedAt')
                    ALTER TABLE dbo.Clients ADD UpdatedAt DATETIME NOT NULL CONSTRAINT DF_Clients_UpdatedAt_Mig DEFAULT (GETDATE());
            END;

            -- Migrate existing ActivityLogs table: add any missing columns
            IF OBJECT_ID(N'dbo.ActivityLogs', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ActivityLogs') AND name = N'ActivityType')
                    ALTER TABLE dbo.ActivityLogs ADD ActivityType NVARCHAR(30) NOT NULL CONSTRAINT DF_ActivityLogs_ActivityType_Mig DEFAULT (N'note');
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ActivityLogs') AND name = N'ActivityDateTime')
                    ALTER TABLE dbo.ActivityLogs ADD ActivityDateTime DATETIME NOT NULL CONSTRAINT DF_ActivityLogs_ActivityDateTime_Mig DEFAULT (GETDATE());
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ActivityLogs') AND name = N'CreatedBy')
                    ALTER TABLE dbo.ActivityLogs ADD CreatedBy NVARCHAR(100) NULL;
            END;

            -- Migrate existing TaskItems table: add any missing columns
            IF OBJECT_ID(N'dbo.TaskItems', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaskItems') AND name = N'Priority')
                    ALTER TABLE dbo.TaskItems ADD Priority NVARCHAR(20) NOT NULL CONSTRAINT DF_TaskItems_Priority_Mig DEFAULT (N'medium');
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaskItems') AND name = N'AssignedTo')
                    ALTER TABLE dbo.TaskItems ADD AssignedTo NVARCHAR(100) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaskItems') AND name = N'DoneAt')
                    ALTER TABLE dbo.TaskItems ADD DoneAt DATETIME NULL;
            END;

            IF OBJECT_ID(N'dbo.ActivityLogs', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ActivityLogs
                (
                    Id               INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    ClientId         INT NOT NULL,
                    ActivityType     NVARCHAR(30) NOT NULL CONSTRAINT DF_ActivityLogs_ActivityType DEFAULT (N'note'),
                    Description      NVARCHAR(MAX) NOT NULL,
                    ActivityDateTime DATETIME NOT NULL CONSTRAINT DF_ActivityLogs_ActivityDateTime DEFAULT (GETDATE()),
                    CreatedBy        NVARCHAR(100) NULL,
                    CreatedAt        DATETIME NOT NULL CONSTRAINT DF_ActivityLogs_CreatedAt DEFAULT (GETDATE())
                );

                CREATE INDEX IX_ActivityLogs_ClientId
                    ON dbo.ActivityLogs(ClientId);
            END;

            IF OBJECT_ID(N'dbo.TaskItems', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.TaskItems
                (
                    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    ClientId    INT NOT NULL,
                    Description NVARCHAR(500) NOT NULL,
                    DueDate     DATETIME NULL,
                    Priority    NVARCHAR(20) NOT NULL CONSTRAINT DF_TaskItems_Priority DEFAULT (N'medium'),
                    AssignedTo  NVARCHAR(100) NULL,
                    IsDone      BIT NOT NULL CONSTRAINT DF_TaskItems_IsDone DEFAULT (0),
                    DoneAt      DATETIME NULL,
                    AddedAt     DATETIME NOT NULL CONSTRAINT DF_TaskItems_AddedAt DEFAULT (GETDATE())
                );

                CREATE INDEX IX_TaskItems_ClientId
                    ON dbo.TaskItems(ClientId);
            END;

            IF OBJECT_ID(N'dbo.StageDefinitions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.StageDefinitions
                (
                    Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Name      NVARCHAR(80) NOT NULL,
                    SortOrder INT NOT NULL,
                    IsActive  BIT NOT NULL CONSTRAINT DF_StageDefinitions_IsActive DEFAULT (1),
                    CreatedAt DATETIME NOT NULL CONSTRAINT DF_StageDefinitions_CreatedAt DEFAULT (GETDATE()),
                    UpdatedAt DATETIME NOT NULL CONSTRAINT DF_StageDefinitions_UpdatedAt DEFAULT (GETDATE())
                );

                CREATE UNIQUE INDEX UX_StageDefinitions_Name
                    ON dbo.StageDefinitions(Name);
                CREATE UNIQUE INDEX UX_StageDefinitions_SortOrder
                    ON dbo.StageDefinitions(SortOrder);
            END;

            IF NOT EXISTS (SELECT 1 FROM dbo.StageDefinitions WHERE IsActive = 1)
            BEGIN
                INSERT INTO dbo.StageDefinitions (Name, SortOrder, IsActive, CreatedAt, UpdatedAt)
                VALUES
                    (N'Reservation', 0, 1, GETDATE(), GETDATE()),
                    (N'Equity Collection', 1, 1, GETDATE(), GETDATE()),
                    (N'Loan Application', 2, 1, GETDATE(), GETDATE()),
                    (N'Document Submission', 3, 1, GETDATE(), GETDATE()),
                    (N'Bank/PI Evaluation', 4, 1, GETDATE(), GETDATE()),
                    (N'Loan Approval', 5, 1, GETDATE(), GETDATE()),
                    (N'Mortgage Signing', 6, 1, GETDATE(), GETDATE()),
                    (N'Takeout Processing', 7, 1, GETDATE(), GETDATE()),
                    (N'Proceeds Released', 8, 1, GETDATE(), GETDATE()),
                    (N'Resolved', 9, 1, GETDATE(), GETDATE());
            END;

            IF EXISTS
            (
                SELECT 1
                FROM sys.check_constraints cc
                WHERE cc.parent_object_id = OBJECT_ID(N'dbo.Clients')
                  AND cc.name = N'CK_Clients_Stage'
            )
            BEGIN
                ALTER TABLE dbo.Clients DROP CONSTRAINT CK_Clients_Stage;
            END;

            IF EXISTS
            (
                SELECT 1
                FROM sys.check_constraints cc
                WHERE cc.parent_object_id = OBJECT_ID(N'dbo.Clients')
                  AND cc.name = N'CK_Clients_FinancingType'
            )
            BEGIN
                ALTER TABLE dbo.Clients DROP CONSTRAINT CK_Clients_FinancingType;
            END;

            IF OBJECT_ID(N'dbo.DelayReasonDefinitions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.DelayReasonDefinitions
                (
                    Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Name      NVARCHAR(120) NOT NULL,
                    SortOrder INT NOT NULL,
                    IsActive  BIT NOT NULL CONSTRAINT DF_DelayReasonDefinitions_IsActive DEFAULT (1),
                    CreatedAt DATETIME NOT NULL CONSTRAINT DF_DelayReasonDefinitions_CreatedAt DEFAULT (GETDATE()),
                    UpdatedAt DATETIME NOT NULL CONSTRAINT DF_DelayReasonDefinitions_UpdatedAt DEFAULT (GETDATE())
                );

                CREATE UNIQUE INDEX UX_DelayReasonDefinitions_Name
                    ON dbo.DelayReasonDefinitions(Name);
                CREATE UNIQUE INDEX UX_DelayReasonDefinitions_SortOrder
                    ON dbo.DelayReasonDefinitions(SortOrder);
            END;

            IF NOT EXISTS (SELECT 1 FROM dbo.DelayReasonDefinitions WHERE IsActive = 1)
            BEGIN
                INSERT INTO dbo.DelayReasonDefinitions (Name, SortOrder, IsActive, CreatedAt, UpdatedAt)
                VALUES
                    (N'None', 0, 1, GETDATE(), GETDATE()),
                    (N'Incomplete Documents', 1, 1, GETDATE(), GETDATE()),
                    (N'Credit Card Disapproval', 2, 1, GETDATE(), GETDATE()),
                    (N'Insufficient Income', 3, 1, GETDATE(), GETDATE()),
                    (N'GCash / E-wallet Issue', 4, 1, GETDATE(), GETDATE()),
                    (N'Member Contribution Shortage', 5, 1, GETDATE(), GETDATE()),
                    (N'Low Appraisal / Appraisal Gap', 6, 1, GETDATE(), GETDATE()),
                    (N'Late Document Compliance', 7, 1, GETDATE(), GETDATE()),
                    (N'Client Unresponsive', 8, 1, GETDATE(), GETDATE()),
                    (N'Client Abroad / OFW', 9, 1, GETDATE(), GETDATE()),
                    (N'Home Credit Issue', 10, 1, GETDATE(), GETDATE());
            END;

            IF OBJECT_ID(N'dbo.FinancingTypeDefinitions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.FinancingTypeDefinitions
                (
                    Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Name      NVARCHAR(60) NOT NULL,
                    SortOrder INT NOT NULL,
                    IsActive  BIT NOT NULL CONSTRAINT DF_FinancingTypeDefinitions_IsActive DEFAULT (1),
                    CreatedAt DATETIME NOT NULL CONSTRAINT DF_FinancingTypeDefinitions_CreatedAt DEFAULT (GETDATE()),
                    UpdatedAt DATETIME NOT NULL CONSTRAINT DF_FinancingTypeDefinitions_UpdatedAt DEFAULT (GETDATE())
                );

                CREATE UNIQUE INDEX UX_FinancingTypeDefinitions_Name
                    ON dbo.FinancingTypeDefinitions(Name);
                CREATE UNIQUE INDEX UX_FinancingTypeDefinitions_SortOrder
                    ON dbo.FinancingTypeDefinitions(SortOrder);
            END;

            IF NOT EXISTS (SELECT 1 FROM dbo.FinancingTypeDefinitions WHERE IsActive = 1)
            BEGIN
                INSERT INTO dbo.FinancingTypeDefinitions (Name, SortOrder, IsActive, CreatedAt, UpdatedAt)
                VALUES
                    (N'Bank', 0, 1, GETDATE(), GETDATE()),
                    (N'Pag-IBIG', 1, 1, GETDATE(), GETDATE()),
                    (N'In-house', 2, 1, GETDATE(), GETDATE()),
                    (N'Cash', 3, 1, GETDATE(), GETDATE());
            END;

            IF OBJECT_ID(N'dbo.ActivityTypeDefinitions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ActivityTypeDefinitions
                (
                    Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Code      NVARCHAR(30) NOT NULL,
                    Label     NVARCHAR(50) NOT NULL,
                    SortOrder INT NOT NULL,
                    IsActive  BIT NOT NULL CONSTRAINT DF_ActivityTypeDefinitions_IsActive DEFAULT (1),
                    CreatedAt DATETIME NOT NULL CONSTRAINT DF_ActivityTypeDefinitions_CreatedAt DEFAULT (GETDATE()),
                    UpdatedAt DATETIME NOT NULL CONSTRAINT DF_ActivityTypeDefinitions_UpdatedAt DEFAULT (GETDATE())
                );

                CREATE UNIQUE INDEX UX_ActivityTypeDefinitions_Code
                    ON dbo.ActivityTypeDefinitions(Code);
                CREATE UNIQUE INDEX UX_ActivityTypeDefinitions_SortOrder
                    ON dbo.ActivityTypeDefinitions(SortOrder);
            END;

            IF NOT EXISTS (SELECT 1 FROM dbo.ActivityTypeDefinitions WHERE IsActive = 1)
            BEGIN
                INSERT INTO dbo.ActivityTypeDefinitions (Code, Label, SortOrder, IsActive, CreatedAt, UpdatedAt)
                VALUES
                    (N'call', N'Call', 0, 1, GETDATE(), GETDATE()),
                    (N'sms', N'SMS', 1, 1, GETDATE(), GETDATE()),
                    (N'email', N'Email', 2, 1, GETDATE(), GETDATE()),
                    (N'meeting', N'Meeting', 3, 1, GETDATE(), GETDATE()),
                    (N'doc', N'Document', 4, 1, GETDATE(), GETDATE()),
                    (N'bank', N'Bank', 5, 1, GETDATE(), GETDATE()),
                    (N'payment', N'Payment', 6, 1, GETDATE(), GETDATE()),
                    (N'stage', N'Stage Update', 7, 1, GETDATE(), GETDATE()),
                    (N'note', N'Note', 8, 1, GETDATE(), GETDATE()),
                    (N'system', N'System', 9, 1, GETDATE(), GETDATE());
            END;

            IF EXISTS
            (
                SELECT 1
                FROM sys.check_constraints cc
                WHERE cc.parent_object_id = OBJECT_ID(N'dbo.ActivityLogs')
                  AND cc.name = N'CK_ActivityLogs_ActivityType'
            )
            BEGIN
                ALTER TABLE dbo.ActivityLogs DROP CONSTRAINT CK_ActivityLogs_ActivityType;
            END;

            IF OBJECT_ID(N'dbo.ClientDelayReasons', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ClientDelayReasons
                (
                    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    ClientId    INT NOT NULL,
                    DelayReason NVARCHAR(120) NOT NULL,
                    SortOrder   INT NOT NULL,
                    CreatedAt   DATETIME NOT NULL CONSTRAINT DF_ClientDelayReasons_CreatedAt DEFAULT (GETDATE())
                );

                CREATE INDEX IX_ClientDelayReasons_ClientId
                    ON dbo.ClientDelayReasons(ClientId);
                CREATE UNIQUE INDEX UX_ClientDelayReasons_ClientId_DelayReason
                    ON dbo.ClientDelayReasons(ClientId, DelayReason);
            END;

            IF NOT EXISTS (SELECT 1 FROM dbo.ClientDelayReasons)
            BEGIN
                INSERT INTO dbo.ClientDelayReasons (ClientId, DelayReason, SortOrder)
                SELECT c.Id, c.DelayReason, 0
                FROM dbo.Clients c
                WHERE c.DelayReason IS NOT NULL
                  AND LTRIM(RTRIM(c.DelayReason)) <> ''
                  AND c.DelayReason <> N'None';

                INSERT INTO dbo.ClientDelayReasons (ClientId, DelayReason, SortOrder)
                SELECT c.Id, c.SecondaryDelayReason, 1
                FROM dbo.Clients c
                WHERE c.SecondaryDelayReason IS NOT NULL
                  AND LTRIM(RTRIM(c.SecondaryDelayReason)) <> ''
                  AND c.SecondaryDelayReason <> N'None'
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.ClientDelayReasons d
                      WHERE d.ClientId = c.Id
                        AND d.DelayReason = c.SecondaryDelayReason
                  );
            END;

            -- ── Stage Bucket Definitions ──
            IF OBJECT_ID(N'dbo.StageBucketDefinitions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.StageBucketDefinitions
                (
                    Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Key]     NVARCHAR(60) NOT NULL,
                    Name      NVARCHAR(120) NOT NULL,
                    SortOrder INT NOT NULL,
                    IsActive  BIT NOT NULL CONSTRAINT DF_StageBucketDef_IsActive DEFAULT (1),
                    AppliesTo NVARCHAR(MAX) NULL,
                    CreatedAt DATETIME NOT NULL CONSTRAINT DF_StageBucketDef_CreatedAt DEFAULT (GETDATE()),
                    UpdatedAt DATETIME NOT NULL CONSTRAINT DF_StageBucketDef_UpdatedAt DEFAULT (GETDATE())
                );

                CREATE UNIQUE INDEX UX_StageBucketDef_Key
                    ON dbo.StageBucketDefinitions([Key]) WHERE IsActive = 1;

                -- Seed default buckets
                INSERT INTO dbo.StageBucketDefinitions ([Key], Name, SortOrder, IsActive, AppliesTo, CreatedAt, UpdatedAt)
                VALUES
                    (N'resolved',      N'Resolved',          0, 1, NULL, GETDATE(), GETDATE()),
                    (N'cancellation',   N'Cancellation',      1, 1, NULL, GETDATE(), GETDATE()),
                    (N'approved',       N'Approved',          2, 1, NULL, GETDATE(), GETDATE()),
                    (N'delivered',      N'Delivered',         3, 1, NULL, GETDATE(), GETDATE()),
                    (N'on-process',     N'On Process',        4, 1, NULL, GETDATE(), GETDATE()),
                    (N'for-process',    N'For Process',       5, 1, NULL, GETDATE(), GETDATE());
            END;

            -- ── Stage Bucket Stages (join table) ──
            IF OBJECT_ID(N'dbo.StageBucketStages', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.StageBucketStages
                (
                    Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    BucketId  INT NOT NULL,
                    StageName NVARCHAR(80) NOT NULL,
                    SortOrder INT NOT NULL CONSTRAINT DF_StageBucketStages_SortOrder DEFAULT (0)
                );

                CREATE INDEX IX_StageBucketStages_BucketId
                    ON dbo.StageBucketStages(BucketId);
                CREATE UNIQUE INDEX UX_StageBucketStages_BucketId_StageName
                    ON dbo.StageBucketStages(BucketId, StageName);

                -- Seed default stage assignments matching frontend constants
                DECLARE @bResolved INT, @bCancellation INT, @bApproved INT, @bDelivered INT, @bOnProcess INT, @bForProcess INT;
                SELECT @bResolved = Id FROM dbo.StageBucketDefinitions WHERE [Key] = N'resolved';
                SELECT @bCancellation = Id FROM dbo.StageBucketDefinitions WHERE [Key] = N'cancellation';
                SELECT @bApproved = Id FROM dbo.StageBucketDefinitions WHERE [Key] = N'approved';
                SELECT @bDelivered = Id FROM dbo.StageBucketDefinitions WHERE [Key] = N'delivered';
                SELECT @bOnProcess = Id FROM dbo.StageBucketDefinitions WHERE [Key] = N'on-process';
                SELECT @bForProcess = Id FROM dbo.StageBucketDefinitions WHERE [Key] = N'for-process';

                INSERT INTO dbo.StageBucketStages (BucketId, StageName, SortOrder) VALUES
                    (@bResolved, N'Resolved', 0),
                    (@bCancellation, N'Cancellation', 0),
                    (@bCancellation, N'Pending Cancellation', 1),
                    (@bCancellation, N'Loan Status: Declined', 2),
                    (@bApproved, N'Loan Approved: Loan Docs Signing/BV/SI', 0),
                    (@bApproved, N'Takeout Processing', 1),
                    (@bDelivered, N'Takeout Processing', 0),
                    (@bOnProcess, N'Loan Approved: Loan Docs Signing/BV/SI', 0),
                    (@bOnProcess, N'Annotation Of Title (Pag-ibig only)', 1),
                    (@bOnProcess, N'Annotated Title Submitted (Pag-ibig only)', 2),
                    (@bOnProcess, N'Takeout Processing', 3),
                    (@bForProcess, N'Client Document Compliance', 0),
                    (@bForProcess, N'Submit to Bank/Pag-IBIG', 1),
                    (@bForProcess, N' Institutional Findings (Optional)', 2);
            END;

            -- ── ActivityLogs: add DelayReason if missing ──
            IF OBJECT_ID(N'dbo.ActivityLogs', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ActivityLogs') AND name = N'DelayReason')
                    ALTER TABLE dbo.ActivityLogs ADD DelayReason NVARCHAR(120) NULL;
            END;

            -- ── Clients: add CreatedBy / ModifiedBy if missing ──
            IF OBJECT_ID(N'dbo.Clients', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'CreatedBy')
                    ALTER TABLE dbo.Clients ADD CreatedBy NVARCHAR(100) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'ModifiedBy')
                    ALTER TABLE dbo.Clients ADD ModifiedBy NVARCHAR(100) NULL;
            END;

            -- ── Drop UNIQUE SortOrder indexes (SortOrder should not be unique) ──
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_StageDefinitions_SortOrder' AND object_id = OBJECT_ID(N'dbo.StageDefinitions'))
                DROP INDEX UX_StageDefinitions_SortOrder ON dbo.StageDefinitions;
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DelayReasonDefinitions_SortOrder' AND object_id = OBJECT_ID(N'dbo.DelayReasonDefinitions'))
                DROP INDEX UX_DelayReasonDefinitions_SortOrder ON dbo.DelayReasonDefinitions;
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_FinancingTypeDefinitions_SortOrder' AND object_id = OBJECT_ID(N'dbo.FinancingTypeDefinitions'))
                DROP INDEX UX_FinancingTypeDefinitions_SortOrder ON dbo.FinancingTypeDefinitions;
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ActivityTypeDefinitions_SortOrder' AND object_id = OBJECT_ID(N'dbo.ActivityTypeDefinitions'))
                DROP INDEX UX_ActivityTypeDefinitions_SortOrder ON dbo.ActivityTypeDefinitions;
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    private static async Task SyncNextActionAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        int clientId,
        CancellationToken cancellationToken)
    {
        const string nextTaskSql = """
            SELECT TOP (1)
                t.Description,
                t.DueDate
            FROM dbo.TaskItems t
            WHERE t.ClientId = @ClientId
              AND t.IsDone = 0
            ORDER BY
                CASE WHEN t.DueDate IS NULL THEN 1 ELSE 0 END,
                t.DueDate ASC,
                CASE LOWER(t.Priority)
                    WHEN 'high' THEN 0
                    WHEN 'medium' THEN 1
                    WHEN 'low' THEN 2
                    ELSE 1
                END,
                t.Id ASC;
            """;

        var next = await connection.QueryFirstOrDefaultAsync<NextActionRow>(
            new CommandDefinition(
                nextTaskSql,
                new { ClientId = clientId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (next is null)
        {
            return;
        }

        const string updateClientSql = """
            UPDATE dbo.Clients
            SET
                NextAction = @NextAction,
                FollowUpDate = @FollowUpDate,
                UpdatedAt = GETDATE()
            WHERE Id = @ClientId;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateClientSql,
                new
                {
                    ClientId = clientId,
                    NextAction = next.Description,
                    FollowUpDate = next.DueDate
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
    }

    private sealed class NextActionRow
    {
        public string Description { get; init; } = string.Empty;
        public DateTime? DueDate { get; init; }
    }

    private sealed class ClientDelayReasonRow
    {
        public int ClientId { get; init; }
        public string DelayReason { get; init; } = string.Empty;
        public int SortOrder { get; init; }
    }

    // ── Projects ──

    public async Task<IReadOnlyList<Project>> GetProjectsAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT p.Id, p.Name, p.SortOrder, p.IsActive, p.CreatedAt, p.UpdatedAt
            FROM dbo.Projects p
            WHERE p.IsActive = 1
            ORDER BY p.SortOrder ASC, p.Id ASC;
            """;

        var rows = await connection.QueryAsync<Project>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<Project?> GetProjectByIdAsync(int projectId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT p.Id, p.Name, p.SortOrder, p.IsActive, p.CreatedAt, p.UpdatedAt
            FROM dbo.Projects p
            WHERE p.Id = @ProjectId;
            """;

        return await connection.QueryFirstOrDefaultAsync<Project>(
            new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateProjectAsync(string name, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            IF EXISTS (SELECT 1 FROM dbo.Projects WHERE IsActive = 1 AND Name = @Name)
            BEGIN
                SELECT CAST(0 AS INT);
                RETURN;
            END

            DECLARE @NextSortOrder INT;
            SET @NextSortOrder = ISNULL(
                (SELECT MAX(SortOrder) + 1 FROM dbo.Projects WHERE IsActive = 1), 0);

            INSERT INTO dbo.Projects (Name, SortOrder, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Name, @NextSortOrder, 1, GETDATE(), GETDATE());

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Name = name.Trim() }, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateProjectAsync(int projectId, string name, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            UPDATE dbo.Projects
            SET Name = @Name, UpdatedAt = GETDATE()
            WHERE Id = @ProjectId AND IsActive = 1;
            """;

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { ProjectId = projectId, Name = name.Trim() }, cancellationToken: cancellationToken));

        return rows > 0;
    }

    public async Task<bool> DeleteProjectAsync(int projectId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            DELETE FROM dbo.Projects WHERE Id = @ProjectId AND IsActive = 1;
            """;

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken));

        return rows > 0;
    }

    // ── Project Units ──

    public async Task<IReadOnlyList<ProjectUnit>> GetProjectUnitsAsync(int projectId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT u.Id, u.ProjectId, u.Name, u.TotalContractPrice, u.SortOrder, u.IsActive, u.CreatedAt, u.UpdatedAt
            FROM dbo.ProjectUnits u
            WHERE u.ProjectId = @ProjectId AND u.IsActive = 1
            ORDER BY u.SortOrder ASC, u.Id ASC;
            """;

        var rows = await connection.QueryAsync<ProjectUnit>(
            new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<ProjectUnit?> GetProjectUnitByIdAsync(int projectId, int unitId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT u.Id, u.ProjectId, u.Name, u.TotalContractPrice, u.SortOrder, u.IsActive, u.CreatedAt, u.UpdatedAt
            FROM dbo.ProjectUnits u
            WHERE u.Id = @UnitId AND u.ProjectId = @ProjectId;
            """;

        return await connection.QueryFirstOrDefaultAsync<ProjectUnit>(
            new CommandDefinition(sql, new { ProjectId = projectId, UnitId = unitId }, cancellationToken: cancellationToken));
    }

    public async Task<ProjectUnit?> GetProjectUnitByUnitIdAsync(int unitId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT u.Id, u.ProjectId, u.Name, u.TotalContractPrice, u.SortOrder, u.IsActive, u.CreatedAt, u.UpdatedAt
            FROM dbo.ProjectUnits u
            WHERE u.Id = @UnitId AND u.IsActive = 1;
            """;

        return await connection.QueryFirstOrDefaultAsync<ProjectUnit>(
            new CommandDefinition(sql, new { UnitId = unitId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateProjectUnitAsync(int projectId, string name, decimal? totalContractPrice, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE Id = @ProjectId AND IsActive = 1)
            BEGIN
                SELECT CAST(0 AS INT);
                RETURN;
            END

            IF EXISTS (SELECT 1 FROM dbo.ProjectUnits WHERE ProjectId = @ProjectId AND IsActive = 1 AND Name = @Name)
            BEGIN
                SELECT CAST(0 AS INT);
                RETURN;
            END

            DECLARE @NextSortOrder INT;
            SET @NextSortOrder = ISNULL(
                (SELECT MAX(SortOrder) + 1 FROM dbo.ProjectUnits WHERE ProjectId = @ProjectId AND IsActive = 1), 0);

            INSERT INTO dbo.ProjectUnits (ProjectId, Name, TotalContractPrice, SortOrder, IsActive, CreatedAt, UpdatedAt)
            VALUES (@ProjectId, @Name, @TotalContractPrice, @NextSortOrder, 1, GETDATE(), GETDATE());

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { ProjectId = projectId, Name = name.Trim(), TotalContractPrice = totalContractPrice }, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateProjectUnitAsync(int projectId, int unitId, string name, decimal? totalContractPrice, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            UPDATE dbo.ProjectUnits
            SET Name = @Name, TotalContractPrice = @TotalContractPrice, UpdatedAt = GETDATE()
            WHERE Id = @UnitId AND ProjectId = @ProjectId AND IsActive = 1;
            """;

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { ProjectId = projectId, UnitId = unitId, Name = name.Trim(), TotalContractPrice = totalContractPrice }, cancellationToken: cancellationToken));

        return rows > 0;
    }

    public async Task<bool> DeleteProjectUnitAsync(int projectId, int unitId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            DELETE FROM dbo.ProjectUnits WHERE Id = @UnitId AND ProjectId = @ProjectId AND IsActive = 1;
            """;

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { ProjectId = projectId, UnitId = unitId }, cancellationToken: cancellationToken));

        return rows > 0;
    }

    public async Task<IReadOnlyList<RecentActivityDto>> GetRecentActivitiesAsync(int limit, int offset, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT
                a.Id,
                a.ClientId,
                c.Name AS ClientName,
                c.Unit,
                p.Name AS ProjectName,
                a.ActivityType,
                a.Description,
                a.ActivityDateTime,
                a.CreatedBy,
                a.CreatedAt
            FROM dbo.ActivityLogs a
            INNER JOIN dbo.Clients c ON c.Id = a.ClientId
            LEFT JOIN dbo.ProjectUnits pu ON pu.Id = c.UnitId
            LEFT JOIN dbo.Projects p ON p.Id = pu.ProjectId
            ORDER BY a.ActivityDateTime DESC
            OFFSET @Offset ROWS
            FETCH NEXT @Limit ROWS ONLY;
            """;

        var results = await connection.QueryAsync<RecentActivityDto>(
            new CommandDefinition(sql, new { Limit = limit, Offset = offset }, cancellationToken: cancellationToken));

        return results.ToList();
    }

    // ── Stage Buckets ──

    public async Task<IReadOnlyList<StageBucketDefinition>> GetStageBucketsAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT Id, [Key], Name, SortOrder, IsActive, AppliesTo, CreatedAt, UpdatedAt
            FROM dbo.StageBucketDefinitions
            WHERE IsActive = 1
            ORDER BY SortOrder ASC, Id ASC;
            """;

        var rows = await connection.QueryAsync<StageBucketDefinition>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<StageBucketDefinition?> GetStageBucketByIdAsync(int bucketId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT Id, [Key], Name, SortOrder, IsActive, AppliesTo, CreatedAt, UpdatedAt
            FROM dbo.StageBucketDefinitions
            WHERE Id = @BucketId;
            """;

        return await connection.QueryFirstOrDefaultAsync<StageBucketDefinition>(
            new CommandDefinition(sql, new { BucketId = bucketId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<StageBucketStage>> GetBucketStagesAsync(int bucketId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT Id, BucketId, StageName, SortOrder
            FROM dbo.StageBucketStages
            WHERE BucketId = @BucketId
            ORDER BY SortOrder ASC, Id ASC;
            """;

        var rows = await connection.QueryAsync<StageBucketStage>(
            new CommandDefinition(sql, new { BucketId = bucketId }, cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<IReadOnlyList<StageBucketStage>> GetAllBucketStagesAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT s.Id, s.BucketId, s.StageName, s.SortOrder
            FROM dbo.StageBucketStages s
            INNER JOIN dbo.StageBucketDefinitions b ON b.Id = s.BucketId
            WHERE b.IsActive = 1
            ORDER BY s.BucketId, s.SortOrder ASC, s.Id ASC;
            """;

        var rows = await connection.QueryAsync<StageBucketStage>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<int> CreateStageBucketAsync(string key, string name, string? appliesToJson, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            IF EXISTS (SELECT 1 FROM dbo.StageBucketDefinitions WHERE IsActive = 1 AND [Key] = @Key)
            BEGIN
                SELECT CAST(0 AS INT);
                RETURN;
            END

            DECLARE @NextSortOrder INT;
            SET @NextSortOrder = ISNULL(
                (SELECT MAX(SortOrder) + 1 FROM dbo.StageBucketDefinitions WHERE IsActive = 1), 0);

            INSERT INTO dbo.StageBucketDefinitions ([Key], Name, SortOrder, IsActive, AppliesTo, CreatedAt, UpdatedAt)
            VALUES (@Key, @Name, @NextSortOrder, 1, @AppliesTo, GETDATE(), GETDATE());

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Key = key.Trim(), Name = name.Trim(), AppliesTo = appliesToJson }, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateStageBucketAsync(int bucketId, string name, string? appliesToJson, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);

        const string sql = """
            UPDATE dbo.StageBucketDefinitions
            SET Name = @Name, AppliesTo = @AppliesTo, UpdatedAt = GETDATE()
            WHERE Id = @BucketId AND IsActive = 1;
            """;

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { BucketId = bucketId, Name = name.Trim(), AppliesTo = appliesToJson }, cancellationToken: cancellationToken));

        return rows > 0;
    }

    public async Task<bool> DeleteStageBucketAsync(int bucketId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string softDeleteSql = """
            UPDATE dbo.StageBucketDefinitions
            SET IsActive = 0, UpdatedAt = GETDATE()
            WHERE Id = @BucketId AND IsActive = 1;
            """;

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(softDeleteSql, new { BucketId = bucketId }, transaction: transaction, cancellationToken: cancellationToken));

        if (rows > 0)
        {
            const string deleteStagesSql = """
                DELETE FROM dbo.StageBucketStages WHERE BucketId = @BucketId;
                """;
            await connection.ExecuteAsync(
                new CommandDefinition(deleteStagesSql, new { BucketId = bucketId }, transaction: transaction, cancellationToken: cancellationToken));
        }

        transaction.Commit();
        return rows > 0;
    }

    public async Task SetBucketStagesAsync(int bucketId, List<string> stages, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string deleteSql = """
            DELETE FROM dbo.StageBucketStages WHERE BucketId = @BucketId;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(deleteSql, new { BucketId = bucketId }, transaction: transaction, cancellationToken: cancellationToken));

        for (var i = 0; i < stages.Count; i++)
        {
            const string insertSql = """
                INSERT INTO dbo.StageBucketStages (BucketId, StageName, SortOrder)
                VALUES (@BucketId, @StageName, @SortOrder);
                """;
            await connection.ExecuteAsync(
                new CommandDefinition(insertSql, new { BucketId = bucketId, StageName = stages[i].Trim(), SortOrder = i }, transaction: transaction, cancellationToken: cancellationToken));
        }

        transaction.Commit();
    }

    public async Task ReorderBucketsAsync(List<int> bucketIds, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureStagesSchemaAsync(connection, cancellationToken);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        for (var i = 0; i < bucketIds.Count; i++)
        {
            const string sql = """
                UPDATE dbo.StageBucketDefinitions
                SET SortOrder = @SortOrder, UpdatedAt = GETDATE()
                WHERE Id = @BucketId AND IsActive = 1;
                """;
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new { BucketId = bucketIds[i], SortOrder = i }, transaction: transaction, cancellationToken: cancellationToken));
        }

        transaction.Commit();
    }
}
