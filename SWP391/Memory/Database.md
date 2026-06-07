/* =====================================================
   DATABASE MAINTENANCE NOTE
=====================================================

   EF Core migrations are the source of truth for incremental
   database updates.

   Recommended for teammates after pulling new migrations:
   dotnet ef database update --project SWP391 --startup-project SWP391

   Generated idempotent SQL script:
   SWP391/Memory/Database_Latest_EF.sql

   This file is a clean-create reference script. It drops and recreates
   ScientificTrendDB, so do not use it for updating a database that has
   data you want to keep.
===================================================== */

/* =====================================================
   DATABASE CREATION
===================================================== */

IF DB_ID('ScientificTrendDB') IS NOT NULL
    DROP DATABASE ScientificTrendDB;
GO

CREATE DATABASE ScientificTrendDB;
GO

USE ScientificTrendDB;
GO


/* =====================================================
   1. USER & AUTH DOMAIN
===================================================== */

CREATE TABLE Roles (
    RoleId INT IDENTITY PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);

INSERT INTO Roles (RoleName)
VALUES
    ('Administrator'),
    ('Researcher'),
    ('Member');

CREATE TABLE Users (
    UserId INT IDENTITY PRIMARY KEY,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(150),
    DateOfBirth DATE NULL,
    PhoneNumber NVARCHAR(20),
    ActorType NVARCHAR(50) NOT NULL DEFAULT N'Student',
    CreatedAt DATETIME2 DEFAULT SYSDATETIME(),
    IsActive BIT DEFAULT 1
);

CREATE TABLE EmailVerificationTokens (
    EmailVerificationTokenId BIGINT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    TokenHash NVARCHAR(255) NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 DEFAULT SYSDATETIME(),
    UsedAt DATETIME2 NULL,

    FOREIGN KEY (UserId)
        REFERENCES Users(UserId)
        ON DELETE CASCADE
);

CREATE TABLE UserRoles (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,

    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),

    FOREIGN KEY (UserId)
        REFERENCES Users(UserId)
        ON DELETE CASCADE,

    FOREIGN KEY (RoleId)
        REFERENCES Roles(RoleId)
        ON DELETE CASCADE
);



/* =====================================================
   2. EXTERNAL API SOURCES
===================================================== */

CREATE TABLE ApiDataSources (
    SourceId INT IDENTITY PRIMARY KEY,
    SourceName NVARCHAR(100) NOT NULL,
    BaseUrl NVARCHAR(500),
    IsActive BIT DEFAULT 1
);



/* =====================================================
   3. ACADEMIC CORE DOMAIN
===================================================== */

CREATE TABLE Journals (
    JournalId INT IDENTITY PRIMARY KEY,
    JournalName NVARCHAR(300) NOT NULL,
    ISSN NVARCHAR(50),
    Publisher NVARCHAR(200)
);

CREATE TABLE Papers (
    PaperId BIGINT IDENTITY PRIMARY KEY,
    Title NVARCHAR(MAX) NOT NULL,
    Abstract NVARCHAR(MAX),
    PublicationYear INT,
    CitationCount INT DEFAULT 0,
    JournalId INT NULL,
    SourceId INT NULL,
    ExternalId NVARCHAR(200),
    CreatedAt DATETIME2 DEFAULT SYSDATETIME(),

    FOREIGN KEY (JournalId)
        REFERENCES Journals(JournalId),

    FOREIGN KEY (SourceId)
        REFERENCES ApiDataSources(SourceId)
);

CREATE TABLE Authors (
    AuthorId INT IDENTITY PRIMARY KEY,
    AuthorName NVARCHAR(200) NOT NULL
);

CREATE TABLE PaperAuthors (
    PaperId BIGINT NOT NULL,
    AuthorId INT NOT NULL,

    CONSTRAINT PK_PaperAuthors PRIMARY KEY (PaperId, AuthorId),

    FOREIGN KEY (PaperId)
        REFERENCES Papers(PaperId)
        ON DELETE CASCADE,

    FOREIGN KEY (AuthorId)
        REFERENCES Authors(AuthorId)
        ON DELETE CASCADE
);

CREATE TABLE ResearchTopics (
    TopicId INT IDENTITY PRIMARY KEY,
    TopicName NVARCHAR(150) NOT NULL UNIQUE,
    Description NVARCHAR(MAX)
);

CREATE TABLE Keywords (
    KeywordId INT IDENTITY PRIMARY KEY,
    KeywordText NVARCHAR(150) NOT NULL UNIQUE,
    TopicId INT NULL,

    FOREIGN KEY (TopicId)
        REFERENCES ResearchTopics(TopicId)
);

CREATE TABLE PaperKeywords (
    PaperId BIGINT NOT NULL,
    KeywordId INT NOT NULL,

    CONSTRAINT PK_PaperKeywords PRIMARY KEY (PaperId, KeywordId),

    FOREIGN KEY (PaperId)
        REFERENCES Papers(PaperId)
        ON DELETE CASCADE,

    FOREIGN KEY (KeywordId)
        REFERENCES Keywords(KeywordId)
        ON DELETE CASCADE
);



/* =====================================================
   4. ANALYTICS DOMAIN
===================================================== */

CREATE TABLE PublicationTrends (
    TrendId BIGINT IDENTITY PRIMARY KEY,
    TopicId INT NULL,
    KeywordId INT NULL,
    TrendYear INT NOT NULL,
    PaperCount INT NOT NULL,
    LastUpdated DATETIME2 DEFAULT SYSDATETIME(),

    FOREIGN KEY (TopicId)
        REFERENCES ResearchTopics(TopicId),

    FOREIGN KEY (KeywordId)
        REFERENCES Keywords(KeywordId)
);

CREATE TABLE TrendSnapshots (
    SnapshotId BIGINT IDENTITY PRIMARY KEY,
    KeywordId INT NULL,
    TopicId INT NULL,
    SnapshotDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    TrendScore FLOAT NOT NULL,
    GrowthRate FLOAT NOT NULL,
    Momentum FLOAT NOT NULL,
    CitationVelocity FLOAT NOT NULL,
    PaperCount INT NOT NULL,
    RecentPaperCount INT NOT NULL,

    FOREIGN KEY (KeywordId)
        REFERENCES Keywords(KeywordId)
        ON DELETE SET NULL,

    FOREIGN KEY (TopicId)
        REFERENCES ResearchTopics(TopicId)
        ON DELETE SET NULL
);



/* =====================================================
   5. PERSONALIZATION DOMAIN
===================================================== */

CREATE TABLE Bookmarks (
    BookmarkId BIGINT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    TargetId BIGINT NOT NULL,
    TargetType NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 DEFAULT SYSDATETIME(),

    FOREIGN KEY (UserId)
        REFERENCES Users(UserId)
        ON DELETE CASCADE
);

CREATE TABLE Follows (
    FollowId BIGINT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    TargetId BIGINT NOT NULL,
    TargetType NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 DEFAULT SYSDATETIME(),

    FOREIGN KEY (UserId)
        REFERENCES Users(UserId)
        ON DELETE CASCADE
);



/* =====================================================
   6. NOTIFICATION SYSTEM
===================================================== */

CREATE TABLE Notifications (
    NotificationId BIGINT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    RelatedId BIGINT NULL,
    RelatedType NVARCHAR(50),
    IsRead BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT SYSDATETIME(),

    FOREIGN KEY (UserId)
        REFERENCES Users(UserId)
        ON DELETE CASCADE
);



/* =====================================================
   7. DATA SYNCHRONIZATION
===================================================== */

CREATE TABLE SyncJobs (
    SyncJobId BIGINT IDENTITY PRIMARY KEY,
    SourceId INT NOT NULL,
    StartTime DATETIME2,
    EndTime DATETIME2,
    Status NVARCHAR(50),
    RecordsFetched INT,
    ErrorMessage NVARCHAR(MAX),

    FOREIGN KEY (SourceId)
        REFERENCES ApiDataSources(SourceId)
);



/* =====================================================
   8. SYSTEM SETTINGS
===================================================== */

CREATE TABLE SystemSettings (
    SettingKey NVARCHAR(100) PRIMARY KEY,
    SettingValue NVARCHAR(MAX)
);



/* =====================================================
   9. PERFORMANCE INDEXES
===================================================== */

CREATE INDEX IX_Papers_Year
ON Papers(PublicationYear);

CREATE INDEX IX_Keywords_Text
ON Keywords(KeywordText);

CREATE INDEX IX_Keywords_TopicId
ON Keywords(TopicId);

CREATE INDEX IX_PublicationTrends_Year
ON PublicationTrends(TrendYear);

CREATE INDEX IX_PublicationTrends_KeywordId
ON PublicationTrends(KeywordId);

CREATE INDEX IX_Trends_TopicYear
ON PublicationTrends(TopicId, TrendYear);

CREATE INDEX IX_Bookmarks_User
ON Bookmarks(UserId);

CREATE INDEX IX_Follows_User
ON Follows(UserId);

CREATE INDEX IX_Notifications_User
ON Notifications(UserId, IsRead);

CREATE INDEX IX_EmailVerificationTokens_TokenHash
ON EmailVerificationTokens(TokenHash);

CREATE INDEX IX_EmailVerificationTokens_User_UsedAt
ON EmailVerificationTokens(UserId, UsedAt);

CREATE INDEX IX_PaperAuthors_AuthorId
ON PaperAuthors(AuthorId);

CREATE INDEX IX_PaperKeywords_KeywordId
ON PaperKeywords(KeywordId);

CREATE INDEX IX_Papers_JournalId
ON Papers(JournalId);

CREATE INDEX IX_Papers_SourceId
ON Papers(SourceId);

CREATE INDEX IX_SyncJobs_SourceId
ON SyncJobs(SourceId);

CREATE INDEX IX_UserRoles_RoleId
ON UserRoles(RoleId);

CREATE INDEX IX_TrendSnapshots_Date
ON TrendSnapshots(SnapshotDate);

CREATE INDEX IX_TrendSnapshots_Keyword_Date
ON TrendSnapshots(KeywordId, SnapshotDate);

CREATE INDEX IX_TrendSnapshots_Topic_Date
ON TrendSnapshots(TopicId, SnapshotDate);

GO
