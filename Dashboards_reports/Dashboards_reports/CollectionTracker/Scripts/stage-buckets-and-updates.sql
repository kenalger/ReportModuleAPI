-- ============================================================================
-- Collection Tracker: New & Updated Tables
-- Run against the Collection Tracker database
-- ============================================================================

-- ── 1. NEW TABLE: StageBucketDefinitions ────────────────────────────────────
IF OBJECT_ID(N'dbo.StageBucketDefinitions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StageBucketDefinitions
    (
        Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Key]     NVARCHAR(60)  NOT NULL,
        Name      NVARCHAR(120) NOT NULL,
        SortOrder INT           NOT NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_StageBucketDef_IsActive DEFAULT (1),
        AppliesTo NVARCHAR(MAX) NULL,          -- JSON array e.g. ["Bank","Pag-IBIG"]
        CreatedAt DATETIME      NOT NULL CONSTRAINT DF_StageBucketDef_CreatedAt DEFAULT (GETDATE()),
        UpdatedAt DATETIME      NOT NULL CONSTRAINT DF_StageBucketDef_UpdatedAt DEFAULT (GETDATE())
    );

    CREATE UNIQUE INDEX UX_StageBucketDef_Key
        ON dbo.StageBucketDefinitions([Key]) WHERE IsActive = 1;

    PRINT 'Created table dbo.StageBucketDefinitions';
END;
GO

-- ── 2. SEED: Default stage buckets ──────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.StageBucketDefinitions)
BEGIN
    INSERT INTO dbo.StageBucketDefinitions ([Key], Name, SortOrder, IsActive, AppliesTo, CreatedAt, UpdatedAt)
    VALUES
        (N'resolved',      N'Resolved',     0, 1, NULL, GETDATE(), GETDATE()),
        (N'cancellation',  N'Cancellation', 1, 1, NULL, GETDATE(), GETDATE()),
        (N'approved',      N'Approved',     2, 1, NULL, GETDATE(), GETDATE()),
        (N'delivered',     N'Delivered',    3, 1, NULL, GETDATE(), GETDATE()),
        (N'on-process',    N'On Process',   4, 1, NULL, GETDATE(), GETDATE()),
        (N'for-process',   N'For Process',  5, 1, NULL, GETDATE(), GETDATE());

    PRINT 'Seeded 6 default stage buckets';
END;
GO

-- ── 3. NEW TABLE: StageBucketStages (join table) ───────────────────────────
IF OBJECT_ID(N'dbo.StageBucketStages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StageBucketStages
    (
        Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BucketId  INT           NOT NULL,
        StageName NVARCHAR(80)  NOT NULL,
        SortOrder INT           NOT NULL CONSTRAINT DF_StageBucketStages_SortOrder DEFAULT (0)
    );

    CREATE INDEX IX_StageBucketStages_BucketId
        ON dbo.StageBucketStages(BucketId);

    CREATE UNIQUE INDEX UX_StageBucketStages_BucketId_StageName
        ON dbo.StageBucketStages(BucketId, StageName);

    PRINT 'Created table dbo.StageBucketStages';
END;
GO

-- ── 4. SEED: Default stage-to-bucket assignments ───────────────────────────
-- Matches actual StageDefinitions from production DB
IF NOT EXISTS (SELECT 1 FROM dbo.StageBucketStages)
BEGIN
    DECLARE @bResolved INT, @bCancellation INT, @bApproved INT,
            @bDelivered INT, @bOnProcess INT, @bForProcess INT;

    SELECT @bResolved     = Id FROM dbo.StageBucketDefinitions WHERE [Key] = N'resolved';
    SELECT @bCancellation = Id FROM dbo.StageBucketDefinitions WHERE [Key] = N'cancellation';
    SELECT @bApproved     = Id FROM dbo.StageBucketDefinitions WHERE [Key] = N'approved';
    SELECT @bDelivered    = Id FROM dbo.StageBucketDefinitions WHERE [Key] = N'delivered';
    SELECT @bOnProcess    = Id FROM dbo.StageBucketDefinitions WHERE [Key] = N'on-process';
    SELECT @bForProcess   = Id FROM dbo.StageBucketDefinitions WHERE [Key] = N'for-process';

    INSERT INTO dbo.StageBucketStages (BucketId, StageName, SortOrder) VALUES
        -- Resolved bucket
        (@bResolved, N'Resolved', 0),

        -- Cancellation bucket
        (@bCancellation, N'Cancellation', 0),
        (@bCancellation, N'Pending Cancellation', 1),
        (@bCancellation, N'Loan Status: Declined', 2),

        -- Approved bucket (loan approved onward)
        (@bApproved, N'Loan Approved: Loan Docs Signing/BV/SI', 0),
        (@bApproved, N'Takeout Processing', 1),

        -- Delivered bucket (fully delivered / proceeds out)
        (@bDelivered, N'Takeout Processing', 0),

        -- On Process bucket (actively being processed by bank)
        (@bOnProcess, N'Loan Approved: Loan Docs Signing/BV/SI', 0),
        (@bOnProcess, N'Annotation Of Title (Pag-ibig only)', 1),
        (@bOnProcess, N'Annotated Title Submitted (Pag-ibig only)', 2),
        (@bOnProcess, N'Takeout Processing', 3),

        -- For Process bucket (early stages — docs being prepared / submitted)
        (@bForProcess, N'Client Document Compliance', 0),
        (@bForProcess, N'Submit to Bank/Pag-IBIG', 1),
        (@bForProcess, N' Institutional Findings (Optional)', 2);

    PRINT 'Seeded default stage bucket assignments';
END;
GO

-- ── 5. ALTER: Add DelayReason column to ActivityLogs ───────────────────────
IF OBJECT_ID(N'dbo.ActivityLogs', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.ActivityLogs')
          AND name = N'DelayReason'
    )
    BEGIN
        ALTER TABLE dbo.ActivityLogs ADD DelayReason NVARCHAR(120) NULL;
        PRINT 'Added DelayReason column to dbo.ActivityLogs';
    END;
END;
GO
