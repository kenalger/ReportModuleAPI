-- ============================================================
-- Collection Tracker – Full Schema + Seed Data
-- Safe to re-run: every statement uses IF NOT EXISTS guards
-- ============================================================

-- ── 1. Projects ─────────────────────────────────────────────
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
GO

-- ── 2. ProjectUnits ─────────────────────────────────────────
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
GO

-- ── 3. Clients ──────────────────────────────────────────────
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
GO

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
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'CreatedBy')
        ALTER TABLE dbo.Clients ADD CreatedBy NVARCHAR(100) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'ModifiedBy')
        ALTER TABLE dbo.Clients ADD ModifiedBy NVARCHAR(100) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'CreatedAt')
        ALTER TABLE dbo.Clients ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_Clients_CreatedAt_Mig DEFAULT (GETDATE());
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Clients') AND name = N'UpdatedAt')
        ALTER TABLE dbo.Clients ADD UpdatedAt DATETIME NOT NULL CONSTRAINT DF_Clients_UpdatedAt_Mig DEFAULT (GETDATE());
END;
GO

-- Drop old check constraints if they exist
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Clients') AND name = N'CK_Clients_Stage')
    ALTER TABLE dbo.Clients DROP CONSTRAINT CK_Clients_Stage;
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Clients') AND name = N'CK_Clients_FinancingType')
    ALTER TABLE dbo.Clients DROP CONSTRAINT CK_Clients_FinancingType;
GO

-- ── 4. ActivityLogs ─────────────────────────────────────────
IF OBJECT_ID(N'dbo.ActivityLogs', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ActivityLogs') AND name = N'ActivityType')
        ALTER TABLE dbo.ActivityLogs ADD ActivityType NVARCHAR(30) NOT NULL CONSTRAINT DF_ActivityLogs_ActivityType_Mig DEFAULT (N'note');
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ActivityLogs') AND name = N'ActivityDateTime')
        ALTER TABLE dbo.ActivityLogs ADD ActivityDateTime DATETIME NOT NULL CONSTRAINT DF_ActivityLogs_ActivityDateTime_Mig DEFAULT (GETDATE());
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ActivityLogs') AND name = N'CreatedBy')
        ALTER TABLE dbo.ActivityLogs ADD CreatedBy NVARCHAR(100) NULL;
END;
GO

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
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.ActivityLogs') AND name = N'CK_ActivityLogs_ActivityType')
    ALTER TABLE dbo.ActivityLogs DROP CONSTRAINT CK_ActivityLogs_ActivityType;
GO

-- ── 5. TaskItems ────────────────────────────────────────────
IF OBJECT_ID(N'dbo.TaskItems', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaskItems') AND name = N'Priority')
        ALTER TABLE dbo.TaskItems ADD Priority NVARCHAR(20) NOT NULL CONSTRAINT DF_TaskItems_Priority_Mig DEFAULT (N'medium');
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaskItems') AND name = N'AssignedTo')
        ALTER TABLE dbo.TaskItems ADD AssignedTo NVARCHAR(100) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaskItems') AND name = N'DoneAt')
        ALTER TABLE dbo.TaskItems ADD DoneAt DATETIME NULL;
END;
GO

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
GO

-- ── 6. StageDefinitions ─────────────────────────────────────
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
GO

-- Seed stages
IF NOT EXISTS (SELECT 1 FROM dbo.StageDefinitions WHERE IsActive = 1)
BEGIN
    INSERT INTO dbo.StageDefinitions (Name, SortOrder, IsActive, CreatedAt, UpdatedAt)
    VALUES
        (N'Reservation',          0, 1, GETDATE(), GETDATE()),
        (N'Equity Collection',    1, 1, GETDATE(), GETDATE()),
        (N'Loan Application',     2, 1, GETDATE(), GETDATE()),
        (N'Document Submission',  3, 1, GETDATE(), GETDATE()),
        (N'Bank/PI Evaluation',   4, 1, GETDATE(), GETDATE()),
        (N'Loan Approval',        5, 1, GETDATE(), GETDATE()),
        (N'Mortgage Signing',     6, 1, GETDATE(), GETDATE()),
        (N'Takeout Processing',   7, 1, GETDATE(), GETDATE()),
        (N'Proceeds Released',    8, 1, GETDATE(), GETDATE()),
        (N'Resolved',             9, 1, GETDATE(), GETDATE());
END;
GO

-- ── 7. DelayReasonDefinitions ───────────────────────────────
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
GO

-- Seed delay reasons
IF NOT EXISTS (SELECT 1 FROM dbo.DelayReasonDefinitions WHERE IsActive = 1)
BEGIN
    INSERT INTO dbo.DelayReasonDefinitions (Name, SortOrder, IsActive, CreatedAt, UpdatedAt)
    VALUES
        (N'None',                           0,  1, GETDATE(), GETDATE()),
        (N'Incomplete Documents',           1,  1, GETDATE(), GETDATE()),
        (N'Credit Card Disapproval',        2,  1, GETDATE(), GETDATE()),
        (N'Insufficient Income',            3,  1, GETDATE(), GETDATE()),
        (N'GCash / E-wallet Issue',         4,  1, GETDATE(), GETDATE()),
        (N'Member Contribution Shortage',   5,  1, GETDATE(), GETDATE()),
        (N'Low Appraisal / Appraisal Gap',  6,  1, GETDATE(), GETDATE()),
        (N'Late Document Compliance',       7,  1, GETDATE(), GETDATE()),
        (N'Client Unresponsive',            8,  1, GETDATE(), GETDATE()),
        (N'Client Abroad / OFW',            9,  1, GETDATE(), GETDATE()),
        (N'Home Credit Issue',              10, 1, GETDATE(), GETDATE());
END;
GO

-- ── 8. FinancingTypeDefinitions ─────────────────────────────
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
GO

-- Seed financing types
IF NOT EXISTS (SELECT 1 FROM dbo.FinancingTypeDefinitions WHERE IsActive = 1)
BEGIN
    INSERT INTO dbo.FinancingTypeDefinitions (Name, SortOrder, IsActive, CreatedAt, UpdatedAt)
    VALUES
        (N'Bank',     0, 1, GETDATE(), GETDATE()),
        (N'Pag-IBIG', 1, 1, GETDATE(), GETDATE()),
        (N'In-house', 2, 1, GETDATE(), GETDATE()),
        (N'Cash',     3, 1, GETDATE(), GETDATE());
END;
GO

-- ── 9. ActivityTypeDefinitions ──────────────────────────────
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
GO

-- Seed activity types
IF NOT EXISTS (SELECT 1 FROM dbo.ActivityTypeDefinitions WHERE IsActive = 1)
BEGIN
    INSERT INTO dbo.ActivityTypeDefinitions (Code, Label, SortOrder, IsActive, CreatedAt, UpdatedAt)
    VALUES
        (N'call',    N'Call',         0, 1, GETDATE(), GETDATE()),
        (N'sms',     N'SMS',          1, 1, GETDATE(), GETDATE()),
        (N'email',   N'Email',        2, 1, GETDATE(), GETDATE()),
        (N'meeting', N'Meeting',      3, 1, GETDATE(), GETDATE()),
        (N'doc',     N'Document',     4, 1, GETDATE(), GETDATE()),
        (N'bank',    N'Bank',         5, 1, GETDATE(), GETDATE()),
        (N'payment', N'Payment',      6, 1, GETDATE(), GETDATE()),
        (N'stage',   N'Stage Update', 7, 1, GETDATE(), GETDATE()),
        (N'note',    N'Note',         8, 1, GETDATE(), GETDATE()),
        (N'system',  N'System',       9, 1, GETDATE(), GETDATE());
END;
GO

-- ── 10. ClientDelayReasons (junction table) ─────────────────
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
GO

-- Back-fill ClientDelayReasons from legacy columns
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
          SELECT 1 FROM dbo.ClientDelayReasons d
          WHERE d.ClientId = c.Id AND d.DelayReason = c.SecondaryDelayReason
      );
END;
GO

PRINT '✓ Collection Tracker schema + seed data applied successfully.';
GO
