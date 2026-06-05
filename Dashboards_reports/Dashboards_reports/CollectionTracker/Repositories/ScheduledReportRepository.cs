using System.Data;
using Dapper;
using Dashboards_reports.CollectionTracker.Data;
using Dashboards_reports.CollectionTracker.Domain;

namespace Dashboards_reports.CollectionTracker.Repositories;

public sealed class ScheduledReportRepository(IDbConnectionFactory connectionFactory) : IScheduledReportRepository
{
    public async Task<IReadOnlyList<ScheduledReport>> GetAllAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureScheduledReportsSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT sr.*, p.Name AS ProjectName
            FROM dbo.ScheduledReports sr
            LEFT JOIN dbo.Projects p ON p.Id = sr.ProjectId
            ORDER BY sr.CreatedAt DESC;
            """;

        var results = await connection.QueryAsync<ScheduledReport>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return results.ToList();
    }

    public async Task<ScheduledReport?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureScheduledReportsSchemaAsync(connection, cancellationToken);

        const string sql = """
            SELECT sr.*, p.Name AS ProjectName
            FROM dbo.ScheduledReports sr
            LEFT JOIN dbo.Projects p ON p.Id = sr.ProjectId
            WHERE sr.Id = @Id;
            """;

        return await connection.QuerySingleOrDefaultAsync<ScheduledReport>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(ScheduledReport report, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureScheduledReportsSchemaAsync(connection, cancellationToken);

        const string sql = """
            INSERT INTO dbo.ScheduledReports
                (Name, ReportType, Frequency, TimeOfDay, DaysOfWeek, DayOfMonth, Recipients, ProjectId, IsActive, CreatedBy)
            VALUES
                (@Name, @ReportType, @Frequency, @TimeOfDay, @DaysOfWeek, @DayOfMonth, @Recipients, @ProjectId, @IsActive, @CreatedBy);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, report, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(int id, ScheduledReport report, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureScheduledReportsSchemaAsync(connection, cancellationToken);

        const string sql = """
            UPDATE dbo.ScheduledReports
            SET Name = @Name,
                ReportType = @ReportType,
                Frequency = @Frequency,
                TimeOfDay = @TimeOfDay,
                DaysOfWeek = @DaysOfWeek,
                DayOfMonth = @DayOfMonth,
                Recipients = @Recipients,
                ProjectId = @ProjectId,
                UpdatedAt = GETDATE()
            WHERE Id = @Id;
            """;

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                Id = id,
                report.Name,
                report.ReportType,
                report.Frequency,
                report.TimeOfDay,
                report.DaysOfWeek,
                report.DayOfMonth,
                report.Recipients,
                report.ProjectId
            }, cancellationToken: cancellationToken));

        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureScheduledReportsSchemaAsync(connection, cancellationToken);

        const string sql = "DELETE FROM dbo.ScheduledReports WHERE Id = @Id;";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return rows > 0;
    }

    public async Task<bool> ToggleActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureScheduledReportsSchemaAsync(connection, cancellationToken);

        const string sql = """
            UPDATE dbo.ScheduledReports
            SET IsActive = @IsActive, UpdatedAt = GETDATE()
            WHERE Id = @Id;
            """;

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, IsActive = isActive }, cancellationToken: cancellationToken));

        return rows > 0;
    }

    public async Task UpdateRunStatusAsync(int id, DateTime runAt, string status, string? errorMessage, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            UPDATE dbo.ScheduledReports
            SET LastRunAt = @RunAt,
                LastRunStatus = @Status,
                LastErrorMessage = @ErrorMessage,
                UpdatedAt = GETDATE()
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, RunAt = runAt, Status = status, ErrorMessage = errorMessage },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ScheduledReport>> GetDueSchedulesAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureScheduledReportsSchemaAsync(connection, cancellationToken);

        // Returns active schedules whose TimeOfDay has passed today and haven't run yet today
        const string sql = """
            SELECT sr.*, p.Name AS ProjectName
            FROM dbo.ScheduledReports sr
            LEFT JOIN dbo.Projects p ON p.Id = sr.ProjectId
            WHERE sr.IsActive = 1
              AND CAST(GETDATE() AS TIME) >= sr.TimeOfDay
              AND (sr.LastRunAt IS NULL OR CAST(sr.LastRunAt AS DATE) < CAST(GETDATE() AS DATE));
            """;

        var results = await connection.QueryAsync<ScheduledReport>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return results.ToList();
    }

    private static async Task EnsureScheduledReportsSchemaAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        const string ddl = """
            IF OBJECT_ID('dbo.ScheduledReports', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ScheduledReports (
                    Id              INT IDENTITY(1,1) PRIMARY KEY,
                    Name            NVARCHAR(200)    NOT NULL,
                    ReportType      NVARCHAR(50)     NOT NULL DEFAULT 'client-risk',
                    Frequency       NVARCHAR(20)     NOT NULL,
                    TimeOfDay       TIME             NOT NULL,
                    DaysOfWeek      NVARCHAR(100)    NULL,
                    DayOfMonth      INT              NULL,
                    Recipients      NVARCHAR(MAX)    NOT NULL,
                    ProjectId       INT              NULL,
                    IsActive        BIT              NOT NULL DEFAULT 1,
                    LastRunAt       DATETIME2        NULL,
                    LastRunStatus   NVARCHAR(50)     NULL,
                    LastErrorMessage NVARCHAR(MAX)   NULL,
                    CreatedAt       DATETIME2        NOT NULL DEFAULT GETDATE(),
                    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETDATE(),
                    CreatedBy       NVARCHAR(200)    NULL
                );
            END

            IF COL_LENGTH('dbo.ScheduledReports', 'ReportType') IS NULL
            BEGIN
                ALTER TABLE dbo.ScheduledReports
                    ADD ReportType NVARCHAR(50) NOT NULL DEFAULT 'client-risk';
            END
            """;

        await connection.ExecuteAsync(new CommandDefinition(ddl, cancellationToken: cancellationToken));
    }
}
