using System.Data;
using Dapper;
using Dashboards_reports.CollectionTracker.Data;
using Dashboards_reports.CollectionTracker.Domain;

namespace Dashboards_reports.CollectionTracker.Repositories;

public sealed class KpiTargetRepository(IDbConnectionFactory connectionFactory) : IKpiTargetRepository
{
    private const int DefaultStuckThresholdDays = 30;
    private const decimal DefaultStuckRateTargetPercent = 5m;
    private const int DefaultLoanCycleTargetDays = 45;

    public async Task<KpiTarget> GetAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureKpiTargetsSchemaAsync(connection, cancellationToken);

        const string sql = "SELECT TOP 1 * FROM dbo.KpiTargets ORDER BY Id;";

        var result = await connection.QuerySingleOrDefaultAsync<KpiTarget>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return result ?? new KpiTarget
        {
            StuckThresholdDays = DefaultStuckThresholdDays,
            StuckRateTargetPercent = DefaultStuckRateTargetPercent,
            LoanCycleTargetDays = DefaultLoanCycleTargetDays,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public async Task<KpiTarget> UpsertAsync(
        int stuckThresholdDays,
        decimal stuckRateTargetPercent,
        int loanCycleTargetDays,
        string? updatedBy,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureKpiTargetsSchemaAsync(connection, cancellationToken);

        const string sql = """
            MERGE dbo.KpiTargets AS target
            USING (SELECT 1 AS Id) AS source
            ON target.Id = source.Id
            WHEN MATCHED THEN
                UPDATE SET StuckThresholdDays      = @StuckThresholdDays,
                           StuckRateTargetPercent  = @StuckRateTargetPercent,
                           LoanCycleTargetDays     = @LoanCycleTargetDays,
                           UpdatedAt               = GETDATE(),
                           UpdatedBy               = @UpdatedBy
            WHEN NOT MATCHED THEN
                INSERT (Id, StuckThresholdDays, StuckRateTargetPercent, LoanCycleTargetDays, UpdatedAt, UpdatedBy)
                VALUES (1,  @StuckThresholdDays, @StuckRateTargetPercent, @LoanCycleTargetDays, GETDATE(), @UpdatedBy);

            SELECT TOP 1 * FROM dbo.KpiTargets ORDER BY Id;
            """;

        return await connection.QuerySingleAsync<KpiTarget>(
            new CommandDefinition(sql, new
            {
                StuckThresholdDays = stuckThresholdDays,
                StuckRateTargetPercent = stuckRateTargetPercent,
                LoanCycleTargetDays = loanCycleTargetDays,
                UpdatedBy = updatedBy
            }, cancellationToken: cancellationToken));
    }

    private static async Task EnsureKpiTargetsSchemaAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        // Migration: old schema (CeiTarget/PtpTarget) is dropped & replaced.
        // Safe because this table only holds one operator-configured row.
        const string ddl = """
            IF OBJECT_ID('dbo.KpiTargets', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.KpiTargets', 'CeiTarget') IS NOT NULL
            BEGIN
                DROP TABLE dbo.KpiTargets;
            END

            IF OBJECT_ID('dbo.KpiTargets', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.KpiTargets (
                    Id                      INT             NOT NULL PRIMARY KEY,
                    StuckThresholdDays      INT             NOT NULL DEFAULT 30,
                    StuckRateTargetPercent  DECIMAL(5,2)    NOT NULL DEFAULT 5.00,
                    LoanCycleTargetDays     INT             NOT NULL DEFAULT 45,
                    UpdatedAt               DATETIME2       NOT NULL DEFAULT GETDATE(),
                    UpdatedBy               NVARCHAR(200)   NULL
                );

                INSERT INTO dbo.KpiTargets (Id, StuckThresholdDays, StuckRateTargetPercent, LoanCycleTargetDays, UpdatedAt, UpdatedBy)
                VALUES (1, 30, 5.00, 45, GETDATE(), NULL);
            END
            """;

        await connection.ExecuteAsync(new CommandDefinition(ddl, cancellationToken: cancellationToken));
    }
}
