-- ============================================
-- Scheduled Reports table for CollectionTrackerDb
-- Run this against: CollectionTrackerDb
-- ============================================

IF OBJECT_ID('dbo.ScheduledReports', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ScheduledReports (
        Id               INT IDENTITY(1,1) PRIMARY KEY,
        Name             NVARCHAR(200)    NOT NULL,
        ReportType       NVARCHAR(50)     NOT NULL DEFAULT 'client-risk',
        Frequency        NVARCHAR(20)     NOT NULL,        -- 'daily','weekly','monthly'
        TimeOfDay        TIME             NOT NULL,
        DaysOfWeek       NVARCHAR(100)    NULL,            -- 'mon,wed,fri' (weekly only)
        DayOfMonth       INT              NULL,            -- 1-28 (monthly only)
        Recipients       NVARCHAR(MAX)    NOT NULL,        -- JSON array of emails
        ProjectId        INT              NULL,
        IsActive         BIT              NOT NULL DEFAULT 1,
        LastRunAt        DATETIME2        NULL,
        LastRunStatus    NVARCHAR(50)     NULL,
        LastErrorMessage NVARCHAR(MAX)    NULL,
        CreatedAt        DATETIME2        NOT NULL DEFAULT GETDATE(),
        UpdatedAt        DATETIME2        NOT NULL DEFAULT GETDATE(),
        CreatedBy        NVARCHAR(200)    NULL
    );

    PRINT 'Table dbo.ScheduledReports created successfully.';
END
ELSE
BEGIN
    PRINT 'Table dbo.ScheduledReports already exists — skipped.';
END
GO
