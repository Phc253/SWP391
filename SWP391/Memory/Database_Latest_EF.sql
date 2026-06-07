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
    ALTER TABLE [Users] ADD [DateOfBirth] date NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN
    ALTER TABLE [Users] ADD [PhoneNumber] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN
    CREATE INDEX [IX_EmailVerificationTokens_TokenHash] ON [EmailVerificationTokens] ([TokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN
    CREATE INDEX [IX_EmailVerificationTokens_User_UsedAt] ON [EmailVerificationTokens] ([UserId], [UsedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN
    CREATE INDEX [IX_TrendSnapshots_Date] ON [TrendSnapshots] ([SnapshotDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN
    CREATE INDEX [IX_TrendSnapshots_Keyword_Date] ON [TrendSnapshots] ([KeywordId], [SnapshotDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604074436_AddTrendSnapshots'
)
BEGIN
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
    ALTER TABLE [Users] ADD [ActorType] nvarchar(50) NOT NULL DEFAULT N'Student';
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

