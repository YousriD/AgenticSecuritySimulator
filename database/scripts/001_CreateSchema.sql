-- AgenticSecuritySimulator MVP schema (SQL Server)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Organizations')
BEGIN
    CREATE TABLE Organizations (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(256) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Twins')
BEGIN
    CREATE TABLE Twins (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        OrganizationId UNIQUEIDENTIFIER NOT NULL REFERENCES Organizations(Id),
        Name NVARCHAR(256) NOT NULL,
        Source NVARCHAR(64) NOT NULL,
        ImportedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Nodes')
BEGIN
    CREATE TABLE Nodes (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TwinId UNIQUEIDENTIFIER NOT NULL REFERENCES Twins(Id) ON DELETE CASCADE,
        ExternalKey NVARCHAR(256) NOT NULL,
        DisplayName NVARCHAR(256) NOT NULL,
        NodeType NVARCHAR(64) NOT NULL,
        Zone NVARCHAR(128) NULL,
        CriticalityWeight DECIMAL(5,4) NOT NULL DEFAULT 0.4,
        PropertiesJson NVARCHAR(MAX) NULL
    );
    CREATE INDEX IX_Nodes_TwinId ON Nodes(TwinId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Edges')
BEGIN
    CREATE TABLE Edges (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TwinId UNIQUEIDENTIFIER NOT NULL REFERENCES Twins(Id) ON DELETE CASCADE,
        FromNodeId UNIQUEIDENTIFIER NOT NULL REFERENCES Nodes(Id),
        ToNodeId UNIQUEIDENTIFIER NOT NULL REFERENCES Nodes(Id),
        Kind NVARCHAR(64) NOT NULL,
        IsSynthetic BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX IX_Edges_TwinId ON Edges(TwinId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AttackScenarios')
BEGIN
    CREATE TABLE AttackScenarios (
        Id NVARCHAR(16) NOT NULL PRIMARY KEY,
        Name NVARCHAR(256) NOT NULL,
        Description NVARCHAR(MAX) NOT NULL,
        CriticalityWeight DECIMAL(5,4) NOT NULL DEFAULT 1.0,
        DefinitionJson NVARCHAR(MAX) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SimulationBatches')
BEGIN
    CREATE TABLE SimulationBatches (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TwinId UNIQUEIDENTIFIER NOT NULL REFERENCES Twins(Id),
        RunCount INT NOT NULL,
        Seed INT NOT NULL,
        ParametersJson NVARCHAR(MAX) NOT NULL,
        ScenarioIdsJson NVARCHAR(512) NOT NULL,
        Status NVARCHAR(32) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CompletedAtUtc DATETIME2 NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SimulationRuns')
BEGIN
    CREATE TABLE SimulationRuns (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        BatchId UNIQUEIDENTIFIER NOT NULL REFERENCES SimulationBatches(Id) ON DELETE CASCADE,
        RunIndex INT NOT NULL,
        ScenarioId NVARCHAR(16) NOT NULL,
        Status NVARCHAR(32) NOT NULL,
        ResilienceScore DECIMAL(8,4) NULL,
        SubScoresJson NVARCHAR(MAX) NULL
    );
    CREATE INDEX IX_SimulationRuns_BatchId ON SimulationRuns(BatchId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SimulationEvents')
BEGIN
    CREATE TABLE SimulationEvents (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL REFERENCES SimulationRuns(Id) ON DELETE CASCADE,
        Sequence INT NOT NULL,
        TimestampOffsetMs INT NOT NULL,
        Actor NVARCHAR(16) NOT NULL,
        TechniqueId NVARCHAR(32) NULL,
        NodeId UNIQUEIDENTIFIER NULL,
        Outcome NVARCHAR(64) NOT NULL,
        Message NVARCHAR(512) NOT NULL
    );
    CREATE INDEX IX_SimulationEvents_RunId ON SimulationEvents(RunId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ResilienceScores')
BEGIN
    CREATE TABLE ResilienceScores (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        RunId UNIQUEIDENTIFIER NOT NULL REFERENCES SimulationRuns(Id) ON DELETE CASCADE,
        Availability DECIMAL(8,4) NOT NULL,
        Detection DECIMAL(8,4) NOT NULL,
        Containment DECIMAL(8,4) NOT NULL,
        Recovery DECIMAL(8,4) NOT NULL,
        BlastRadius DECIMAL(8,4) NOT NULL,
        CompositeScore DECIMAL(8,4) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BatchStatistics')
BEGIN
    CREATE TABLE BatchStatistics (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        BatchId UNIQUEIDENTIFIER NOT NULL REFERENCES SimulationBatches(Id) ON DELETE CASCADE,
        MeanScore DECIMAL(8,4) NOT NULL,
        P10Score DECIMAL(8,4) NOT NULL,
        P90Score DECIMAL(8,4) NOT NULL,
        WeakestDimension NVARCHAR(64) NOT NULL,
        WeakestDimensionPct DECIMAL(8,4) NOT NULL,
        StatsJson NVARCHAR(MAX) NOT NULL
    );
END
GO
