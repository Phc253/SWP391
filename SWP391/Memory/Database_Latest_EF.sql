IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [ApiDataSources] (
        [SourceId] int NOT NULL IDENTITY,
        [SourceName] nvarchar(100) NOT NULL,
        [BaseUrl] nvarchar(500) NULL,
        [IsActive] bit NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK__ApiDataS__16E0191954850B1E] PRIMARY KEY ([SourceId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [Authors] (
        [AuthorId] int NOT NULL IDENTITY,
        [AuthorName] nvarchar(200) NOT NULL,
        CONSTRAINT [PK__Authors__70DAFC347F30D68E] PRIMARY KEY ([AuthorId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [Journals] (
        [JournalId] int NOT NULL IDENTITY,
        [JournalName] nvarchar(300) NOT NULL,
        [ISSN] nvarchar(50) NULL,
        [Publisher] nvarchar(200) NULL,
        CONSTRAINT [PK__Journals__250103E6EDD19799] PRIMARY KEY ([JournalId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [ResearchTopics] (
        [TopicId] int NOT NULL IDENTITY,
        [TopicName] nvarchar(150) NOT NULL,
        [Description] nvarchar(max) NULL,
        CONSTRAINT [PK__Research__022E0F5D6DD9197C] PRIMARY KEY ([TopicId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [RoleId] int NOT NULL IDENTITY,
        [RoleName] nvarchar(50) NOT NULL,
        CONSTRAINT [PK__Roles__8AFACE1AAEFBCBC7] PRIMARY KEY ([RoleId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [SystemSettings] (
        [SettingKey] nvarchar(100) NOT NULL,
        [SettingValue] nvarchar(max) NULL,
        CONSTRAINT [PK__SystemSe__01E719AC4ECCE7D3] PRIMARY KEY ([SettingKey])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [UserId] int NOT NULL IDENTITY,
        [Email] nvarchar(255) NOT NULL,
        [PasswordHash] nvarchar(255) NOT NULL,
        [FullName] nvarchar(150) NULL,
        [CreatedAt] datetime2 NULL DEFAULT ((sysdatetime())),
        [IsActive] bit NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK__Users__1788CC4CABBA0262] PRIMARY KEY ([UserId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [SyncJobs] (
        [SyncJobId] bigint NOT NULL IDENTITY,
        [SourceId] int NOT NULL,
        [StartTime] datetime2 NULL,
        [EndTime] datetime2 NULL,
        [Status] nvarchar(50) NULL,
        [RecordsFetched] int NULL,
        [ErrorMessage] nvarchar(max) NULL,
        CONSTRAINT [PK__SyncJobs__1078C047AFB584C5] PRIMARY KEY ([SyncJobId]),
        CONSTRAINT [FK__SyncJobs__Source__5AEE82B9] FOREIGN KEY ([SourceId]) REFERENCES [ApiDataSources] ([SourceId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [Papers] (
        [PaperId] bigint NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [Abstract] nvarchar(max) NULL,
        [PublicationYear] int NULL,
        [CitationCount] int NULL DEFAULT 0,
        [JournalId] int NULL,
        [SourceId] int NULL,
        [ExternalId] nvarchar(200) NULL,
        [CreatedAt] datetime2 NULL DEFAULT ((sysdatetime())),
        CONSTRAINT [PK__Papers__AB86120B6AA80FBB] PRIMARY KEY ([PaperId]),
        CONSTRAINT [FK__Papers__JournalI__35BCFE0A] FOREIGN KEY ([JournalId]) REFERENCES [Journals] ([JournalId]),
        CONSTRAINT [FK__Papers__SourceId__36B12243] FOREIGN KEY ([SourceId]) REFERENCES [ApiDataSources] ([SourceId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [Keywords] (
        [KeywordId] int NOT NULL IDENTITY,
        [KeywordText] nvarchar(150) NOT NULL,
        [TopicId] int NULL,
        CONSTRAINT [PK__Keywords__37C13521B41C6024] PRIMARY KEY ([KeywordId]),
        CONSTRAINT [FK__Keywords__TopicI__4316F928] FOREIGN KEY ([TopicId]) REFERENCES [ResearchTopics] ([TopicId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [Bookmarks] (
        [BookmarkId] bigint NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TargetId] bigint NOT NULL,
        [TargetType] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NULL DEFAULT ((sysdatetime())),
        CONSTRAINT [PK__Bookmark__541A3B71063868DC] PRIMARY KEY ([BookmarkId]),
        CONSTRAINT [FK__Bookmarks__UserI__4F7CD00D] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [Follows] (
        [FollowId] bigint NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TargetId] bigint NOT NULL,
        [TargetType] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NULL DEFAULT ((sysdatetime())),
        CONSTRAINT [PK__Follows__2CE810AE40649A39] PRIMARY KEY ([FollowId]),
        CONSTRAINT [FK__Follows__UserId__534D60F1] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [Notifications] (
        [NotificationId] bigint NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [RelatedId] bigint NULL,
        [RelatedType] nvarchar(50) NULL,
        [IsRead] bit NULL DEFAULT CAST(0 AS bit),
        [CreatedAt] datetime2 NULL DEFAULT ((sysdatetime())),
        CONSTRAINT [PK__Notifica__20CF2E12AD2102BE] PRIMARY KEY ([NotificationId]),
        CONSTRAINT [FK__Notificat__UserI__5812160E] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK__UserRoles__RoleI__2D27B809] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([RoleId]) ON DELETE CASCADE,
        CONSTRAINT [FK__UserRoles__UserI__2C3393D0] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [PaperAuthors] (
        [PaperId] bigint NOT NULL,
        [AuthorId] int NOT NULL,
        CONSTRAINT [PK_PaperAuthors] PRIMARY KEY ([PaperId], [AuthorId]),
        CONSTRAINT [FK__PaperAuth__Autho__3C69FB99] FOREIGN KEY ([AuthorId]) REFERENCES [Authors] ([AuthorId]) ON DELETE CASCADE,
        CONSTRAINT [FK__PaperAuth__Paper__3B75D760] FOREIGN KEY ([PaperId]) REFERENCES [Papers] ([PaperId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [PaperKeywords] (
        [PaperId] bigint NOT NULL,
        [KeywordId] int NOT NULL,
        CONSTRAINT [PK_PaperKeywords] PRIMARY KEY ([PaperId], [KeywordId]),
        CONSTRAINT [FK__PaperKeyw__Keywo__46E78A0C] FOREIGN KEY ([KeywordId]) REFERENCES [Keywords] ([KeywordId]) ON DELETE CASCADE,
        CONSTRAINT [FK__PaperKeyw__Paper__45F365D3] FOREIGN KEY ([PaperId]) REFERENCES [Papers] ([PaperId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE TABLE [PublicationTrends] (
        [TrendId] bigint NOT NULL IDENTITY,
        [TopicId] int NULL,
        [KeywordId] int NULL,
        [TrendYear] int NOT NULL,
        [PaperCount] int NOT NULL,
        [LastUpdated] datetime2 NULL DEFAULT ((sysdatetime())),
        CONSTRAINT [PK__Publicat__DACD10F79107C7D0] PRIMARY KEY ([TrendId]),
        CONSTRAINT [FK__Publicati__Keywo__4BAC3F29] FOREIGN KEY ([KeywordId]) REFERENCES [Keywords] ([KeywordId]),
        CONSTRAINT [FK__Publicati__Topic__4AB81AF0] FOREIGN KEY ([TopicId]) REFERENCES [ResearchTopics] ([TopicId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookmarks_User] ON [Bookmarks] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Follows_User] ON [Follows] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Keywords_Text] ON [Keywords] ([KeywordText]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Keywords_TopicId] ON [Keywords] ([TopicId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ__Keywords__219EE3D704701796] ON [Keywords] ([KeywordText]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_User] ON [Notifications] ([UserId], [IsRead]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PaperAuthors_AuthorId] ON [PaperAuthors] ([AuthorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PaperKeywords_KeywordId] ON [PaperKeywords] ([KeywordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Papers_JournalId] ON [Papers] ([JournalId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Papers_SourceId] ON [Papers] ([SourceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Papers_Year] ON [Papers] ([PublicationYear]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PublicationTrends_KeywordId] ON [PublicationTrends] ([KeywordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PublicationTrends_Year] ON [PublicationTrends] ([TrendYear]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Trends_TopicYear] ON [PublicationTrends] ([TopicId], [TrendYear]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ__Research__6C795E8C662E12F0] ON [ResearchTopics] ([TopicName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ__Roles__8A2B61607F418F30] ON [Roles] ([RoleName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SyncJobs_SourceId] ON [SyncJobs] ([SourceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ__Users__A9D105340115A41D] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033337_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260604033337_InitialCreate', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033645_SeedDefaultRoles'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] ON;
    EXEC(N'INSERT INTO [Roles] ([RoleId], [RoleName])
    VALUES (1, N''Administrator''),
    (2, N''Researcher''),
    (3, N''Member'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604033645_SeedDefaultRoles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260604033645_SeedDefaultRoles', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN

    IF COL_LENGTH('Users', 'DateOfBirth') IS NULL
        ALTER TABLE [Users] ADD [DateOfBirth] date NULL;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN

    IF COL_LENGTH('Users', 'PhoneNumber') IS NULL
        ALTER TABLE [Users] ADD [PhoneNumber] nvarchar(20) NULL;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN

    IF OBJECT_ID(N'[EmailVerificationTokens]', N'U') IS NULL
    BEGIN
        CREATE TABLE [EmailVerificationTokens] (
            [EmailVerificationTokenId] bigint NOT NULL IDENTITY,
            [UserId] int NOT NULL,
            [TokenHash] nvarchar(255) NOT NULL,
            [ExpiresAt] datetime2 NOT NULL,
            [CreatedAt] datetime2 NOT NULL DEFAULT ((sysdatetime())),
            [UsedAt] datetime2 NULL,
            CONSTRAINT [PK_EmailVerificationTokens] PRIMARY KEY ([EmailVerificationTokenId]),
            CONSTRAINT [FK_EmailVerificationTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
        );
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN

    IF OBJECT_ID(N'[TrendSnapshots]', N'U') IS NULL
    BEGIN
        CREATE TABLE [TrendSnapshots] (
            [SnapshotId] bigint NOT NULL IDENTITY,
            [KeywordId] int NULL,
            [TopicId] int NULL,
            [SnapshotDate] datetime2 NOT NULL DEFAULT ((sysdatetime())),
            [TrendScore] float NOT NULL,
            [GrowthRate] float NOT NULL,
            [Momentum] float NOT NULL,
            [CitationVelocity] float NOT NULL,
            [PaperCount] int NOT NULL,
            [RecentPaperCount] int NOT NULL,
            CONSTRAINT [PK_TrendSnapshots] PRIMARY KEY ([SnapshotId]),
            CONSTRAINT [FK_TrendSnapshots_Keywords_KeywordId] FOREIGN KEY ([KeywordId]) REFERENCES [Keywords] ([KeywordId]) ON DELETE SET NULL,
            CONSTRAINT [FK_TrendSnapshots_ResearchTopics_TopicId] FOREIGN KEY ([TopicId]) REFERENCES [ResearchTopics] ([TopicId]) ON DELETE SET NULL
        );
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN

    IF OBJECT_ID(N'[EmailVerificationTokens]', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmailVerificationTokens_TokenHash' AND object_id = OBJECT_ID(N'[EmailVerificationTokens]'))
        CREATE INDEX [IX_EmailVerificationTokens_TokenHash] ON [EmailVerificationTokens] ([TokenHash]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN

    IF OBJECT_ID(N'[EmailVerificationTokens]', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmailVerificationTokens_User_UsedAt' AND object_id = OBJECT_ID(N'[EmailVerificationTokens]'))
        CREATE INDEX [IX_EmailVerificationTokens_User_UsedAt] ON [EmailVerificationTokens] ([UserId], [UsedAt]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN

    IF OBJECT_ID(N'[TrendSnapshots]', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TrendSnapshots_Date' AND object_id = OBJECT_ID(N'[TrendSnapshots]'))
        CREATE INDEX [IX_TrendSnapshots_Date] ON [TrendSnapshots] ([SnapshotDate]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN

    IF OBJECT_ID(N'[TrendSnapshots]', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TrendSnapshots_Keyword_Date' AND object_id = OBJECT_ID(N'[TrendSnapshots]'))
        CREATE INDEX [IX_TrendSnapshots_Keyword_Date] ON [TrendSnapshots] ([KeywordId], [SnapshotDate]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN

    IF OBJECT_ID(N'[TrendSnapshots]', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TrendSnapshots_Topic_Date' AND object_id = OBJECT_ID(N'[TrendSnapshots]'))
        CREATE INDEX [IX_TrendSnapshots_Topic_Date] ON [TrendSnapshots] ([TopicId], [SnapshotDate]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260604074436_AddTrendSnapshots', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605122510_AddUserActorType'
)
BEGIN

    IF COL_LENGTH('Users', 'ActorType') IS NULL
        ALTER TABLE [Users] ADD [ActorType] nvarchar(50) NOT NULL CONSTRAINT [DF_Users_ActorType] DEFAULT N'Student';

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605122510_AddUserActorType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260605122510_AddUserActorType', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611000247_AddActivityLog'
)
BEGIN

    IF OBJECT_ID(N'[ActivityLogs]', N'U') IS NULL
    BEGIN
        CREATE TABLE [ActivityLogs] (
            [ActivityLogId] bigint NOT NULL IDENTITY,
            [UserId] int NULL,
            [Action] nvarchar(100) NOT NULL,
            [TargetType] nvarchar(50) NULL,
            [TargetId] bigint NULL,
            [Details] nvarchar(max) NULL,
            [IpAddress] nvarchar(45) NULL,
            [CreatedAt] datetime2 NOT NULL DEFAULT ((sysdatetime())),
            CONSTRAINT [PK_ActivityLogs] PRIMARY KEY ([ActivityLogId]),
            CONSTRAINT [FK_ActivityLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE SET NULL
        );
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611000247_AddActivityLog'
)
BEGIN

    IF OBJECT_ID(N'[ActivityLogs]', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityLogs_CreatedAt' AND object_id = OBJECT_ID(N'[ActivityLogs]'))
        CREATE INDEX [IX_ActivityLogs_CreatedAt] ON [ActivityLogs] ([CreatedAt]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611000247_AddActivityLog'
)
BEGIN

    IF OBJECT_ID(N'[ActivityLogs]', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityLogs_UserId' AND object_id = OBJECT_ID(N'[ActivityLogs]'))
        CREATE INDEX [IX_ActivityLogs_UserId] ON [ActivityLogs] ([UserId]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611000247_AddActivityLog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260611000247_AddActivityLog', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [Bookmarks] DROP CONSTRAINT [FK__Bookmarks__UserI__4F7CD00D];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [Follows] DROP CONSTRAINT [FK__Follows__UserId__534D60F1];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [Notifications] DROP CONSTRAINT [FK__Notificat__UserI__5812160E];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [PaperAuthors] DROP CONSTRAINT [FK__PaperAuth__Autho__3C69FB99];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [PaperAuthors] DROP CONSTRAINT [FK__PaperAuth__Paper__3B75D760];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DROP INDEX [UQ__Research__6C795E8C662E12F0] ON [ResearchTopics];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [PaperAuthors] DROP CONSTRAINT [PK_PaperAuthors];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DROP INDEX [UQ__Keywords__219EE3D704701796] ON [Keywords];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SyncJobs]') AND [c].[name] = N'SourceId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [SyncJobs] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [SyncJobs] ALTER COLUMN [SourceId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ResearchTopics]') AND [c].[name] = N'TopicName');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [ResearchTopics] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [ResearchTopics] ALTER COLUMN [TopicName] nvarchar(150) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PublicationTrends]') AND [c].[name] = N'TrendYear');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [PublicationTrends] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [PublicationTrends] ALTER COLUMN [TrendYear] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PublicationTrends]') AND [c].[name] = N'PaperCount');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [PublicationTrends] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [PublicationTrends] ALTER COLUMN [PaperCount] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Papers]') AND [c].[name] = N'Title');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Papers] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [Papers] ALTER COLUMN [Title] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [PaperAuthors] ADD [Affiliation] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [PaperAuthors] ADD [AuthorOrder] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [PaperAuthors] ADD [IsCorresponding] bit NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'UserId');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Notifications] ALTER COLUMN [UserId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'Message');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [Notifications] ALTER COLUMN [Message] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Keywords]') AND [c].[name] = N'KeywordText');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Keywords] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [Keywords] ALTER COLUMN [KeywordText] nvarchar(150) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Journals]') AND [c].[name] = N'JournalName');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Journals] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [Journals] ALTER COLUMN [JournalName] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [Journals] ADD [ContactEmail] nvarchar(255) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [Journals] ADD [ImpactFactor] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [Journals] ADD [Website] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Follows]') AND [c].[name] = N'UserId');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Follows] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [Follows] ALTER COLUMN [UserId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Follows]') AND [c].[name] = N'TargetType');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Follows] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [Follows] ALTER COLUMN [TargetType] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Follows]') AND [c].[name] = N'TargetId');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Follows] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [Follows] ALTER COLUMN [TargetId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookmarks]') AND [c].[name] = N'UserId');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Bookmarks] DROP CONSTRAINT [' + @var12 + '];');
    ALTER TABLE [Bookmarks] ALTER COLUMN [UserId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var13 sysname;
    SELECT @var13 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookmarks]') AND [c].[name] = N'TargetType');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Bookmarks] DROP CONSTRAINT [' + @var13 + '];');
    ALTER TABLE [Bookmarks] ALTER COLUMN [TargetType] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var14 sysname;
    SELECT @var14 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookmarks]') AND [c].[name] = N'TargetId');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Bookmarks] DROP CONSTRAINT [' + @var14 + '];');
    ALTER TABLE [Bookmarks] ALTER COLUMN [TargetId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var15 sysname;
    SELECT @var15 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Authors]') AND [c].[name] = N'AuthorName');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Authors] DROP CONSTRAINT [' + @var15 + '];');
    ALTER TABLE [Authors] ALTER COLUMN [AuthorName] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [Authors] ADD [ResearchArea] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [Authors] ADD [TotalPublications] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    DECLARE @var16 sysname;
    SELECT @var16 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ApiDataSources]') AND [c].[name] = N'SourceName');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [ApiDataSources] DROP CONSTRAINT [' + @var16 + '];');
    ALTER TABLE [ApiDataSources] ALTER COLUMN [SourceName] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [PaperAuthors] ADD CONSTRAINT [PK__PaperAut__FC8BBDC843D72F9A] PRIMARY KEY ([PaperId], [AuthorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    CREATE TABLE [DashboardReports] (
        [ReportId] bigint NOT NULL IDENTITY,
        [UserId] int NULL,
        [ReportName] nvarchar(200) NULL,
        [ReportType] nvarchar(50) NULL,
        [FilterConfig] nvarchar(max) NULL,
        [GeneratedAt] datetime2 NULL,
        CONSTRAINT [PK__Dashboar__D5BD48054FB0E0D0] PRIMARY KEY ([ReportId]),
        CONSTRAINT [FK__Dashboard__UserI__5070F446] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    CREATE TABLE [PaperCitations] (
        [CitationId] bigint NOT NULL IDENTITY,
        [CitingPaperId] bigint NOT NULL,
        [CitedPaperId] bigint NOT NULL,
        [CreatedAt] datetime2 NULL,
        CONSTRAINT [PK__PaperCit__EAD2ADFB7C803DC4] PRIMARY KEY ([CitationId]),
        CONSTRAINT [FK__PaperCita__Cited__628FA481] FOREIGN KEY ([CitedPaperId]) REFERENCES [Papers] ([PaperId]),
        CONSTRAINT [FK__PaperCita__Citin__619B8048] FOREIGN KEY ([CitingPaperId]) REFERENCES [Papers] ([PaperId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    CREATE TABLE [ResearchGroups] (
        [GroupId] int NOT NULL IDENTITY,
        [GroupName] nvarchar(200) NOT NULL,
        [OwnerId] int NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAt] datetime2 NULL,
        CONSTRAINT [PK__Research__149AF36A1F2F87A7] PRIMARY KEY ([GroupId]),
        CONSTRAINT [FK__ResearchG__Owner__656C112C] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([UserId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    CREATE TABLE [UserPreferences] (
        [PreferenceId] bigint NOT NULL IDENTITY,
        [UserId] int NULL,
        [PreferredField] nvarchar(150) NULL,
        [PreferredYearRange] nvarchar(50) NULL,
        [NotificationFrequency] nvarchar(50) NULL,
        [CreatedAt] datetime2 NULL,
        CONSTRAINT [PK__UserPref__E228496F80308D5A] PRIMARY KEY ([PreferenceId]),
        CONSTRAINT [FK__UserPrefe__UserI__2E1BDC42] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    CREATE TABLE [GroupMembers] (
        [GroupId] int NOT NULL,
        [UserId] int NOT NULL,
        [RoleInGroup] nvarchar(50) NULL,
        [JoinedAt] datetime2 NULL,
        CONSTRAINT [PK__GroupMem__C5E27FAE44EE6647] PRIMARY KEY ([GroupId], [UserId]),
        CONSTRAINT [FK__GroupMemb__Group__68487DD7] FOREIGN KEY ([GroupId]) REFERENCES [ResearchGroups] ([GroupId]),
        CONSTRAINT [FK__GroupMemb__UserI__693CA210] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ__Research__6C795E8C662E12F0] ON [ResearchTopics] ([TopicName]) WHERE [TopicName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ__Keywords__219EE3D704701796] ON [Keywords] ([KeywordText]) WHERE [KeywordText] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    CREATE INDEX [IX_DashboardReports_UserId] ON [DashboardReports] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    CREATE INDEX [IX_GroupMembers_UserId] ON [GroupMembers] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    CREATE INDEX [IX_PaperCitations_CitedPaperId] ON [PaperCitations] ([CitedPaperId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    CREATE INDEX [IX_PaperCitations_CitingPaperId] ON [PaperCitations] ([CitingPaperId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    CREATE INDEX [IX_ResearchGroups_OwnerId] ON [ResearchGroups] ([OwnerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    CREATE INDEX [IX_UserPreferences_UserId] ON [UserPreferences] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [Bookmarks] ADD CONSTRAINT [FK__Bookmarks__UserI__4F7CD00D] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [Follows] ADD CONSTRAINT [FK__Follows__UserId__534D60F1] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [Notifications] ADD CONSTRAINT [FK__Notificat__UserI__5812160E] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [PaperAuthors] ADD CONSTRAINT [FK__PaperAuth__Autho__3C69FB99] FOREIGN KEY ([AuthorId]) REFERENCES [Authors] ([AuthorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    ALTER TABLE [PaperAuthors] ADD CONSTRAINT [FK__PaperAuth__Paper__3B75D760] FOREIGN KEY ([PaperId]) REFERENCES [Papers] ([PaperId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614201637_AddDashboardReports'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614201637_AddDashboardReports', N'8.0.10');
END;
GO

COMMIT;
GO

