-- ============================================
-- Add ReportType column to ScheduledReports
-- Run this against: CollectionTrackerDb
-- ============================================

IF COL_LENGTH('dbo.ScheduledReports', 'ReportType') IS NULL
BEGIN
    ALTER TABLE dbo.ScheduledReports
        ADD ReportType NVARCHAR(50) NOT NULL DEFAULT 'client-risk';

    PRINT 'Column ReportType added to dbo.ScheduledReports.';
END
ELSE
BEGIN
    PRINT 'Column ReportType already exists — skipped.';
END
GO
