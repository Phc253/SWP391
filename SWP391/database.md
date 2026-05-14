/* =====================================================
   DATABASE
===================================================== */

IF DB_ID('ScientificTrendDB') IS NOT NULL
    DROP DATABASE ScientificTrendDB;
GO

CREATE DATABASE ScientificTrendDB;
GO

USE ScientificTrendDB;
GO


/* =====================================================
   USER & AUTH DOMAIN
===================================================== */

CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(150),
    CreatedAt DATETIME2,
    IsActive BIT
);

CREATE TABLE UserRoles (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);


/* =====================================================
   USER PERSONALIZATION
===================================================== */

CREATE TABLE UserPreferences (
    PreferenceId BIGINT IDENTITY PRIMARY KEY,
    UserId INT,
    PreferredField NVARCHAR(150),
    PreferredYearRange NVARCHAR(50),
    NotificationFrequency NVARCHAR(50),
    CreatedAt DATETIME2,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);


/* =====================================================
   API SOURCES
===================================================== */

CREATE TABLE ApiDataSources (
    SourceId INT IDENTITY PRIMARY KEY,
    SourceName NVARCHAR(100),
    BaseUrl NVARCHAR(500),
    IsActive BIT
);


/* =====================================================
   ACADEMIC CORE
===================================================== */

CREATE TABLE Journals (
    JournalId INT IDENTITY PRIMARY KEY,
    JournalName NVARCHAR(300),
    ISSN NVARCHAR(50),
    Publisher NVARCHAR(200),
    ContactEmail NVARCHAR(255),
    Website NVARCHAR(500),
    ImpactFactor FLOAT
);

CREATE TABLE Papers (
    PaperId BIGINT IDENTITY PRIMARY KEY,
    Title NVARCHAR(MAX),
    Abstract NVARCHAR(MAX),
    PublicationYear INT,
    JournalId INT,
    SourceId INT,
    ExternalId NVARCHAR(200),
    CreatedAt DATETIME2,
    FOREIGN KEY (JournalId) REFERENCES Journals(JournalId),
    FOREIGN KEY (SourceId) REFERENCES ApiDataSources(SourceId)
);

CREATE TABLE Authors (
    AuthorId INT IDENTITY PRIMARY KEY,
    AuthorName NVARCHAR(200),
    TotalPublications INT,
    ResearchArea NVARCHAR(300)
);

CREATE TABLE PaperAuthors (
    PaperId BIGINT NOT NULL,
    AuthorId INT NOT NULL,
    AuthorOrder INT,
    IsCorresponding BIT,
    Affiliation NVARCHAR(300),
    PRIMARY KEY (PaperId, AuthorId),
    FOREIGN KEY (PaperId) REFERENCES Papers(PaperId),
    FOREIGN KEY (AuthorId) REFERENCES Authors(AuthorId)
);


/* =====================================================
   TOPICS & KEYWORDS
===================================================== */

CREATE TABLE ResearchTopics (
    TopicId INT IDENTITY PRIMARY KEY,
    TopicName NVARCHAR(150) UNIQUE,
    Description NVARCHAR(MAX)
);

CREATE TABLE Keywords (
    KeywordId INT IDENTITY PRIMARY KEY,
    KeywordText NVARCHAR(150) UNIQUE,
    TopicId INT,
    FOREIGN KEY (TopicId) REFERENCES ResearchTopics(TopicId)
);

CREATE TABLE PaperKeywords (
    PaperId BIGINT NOT NULL,
    KeywordId INT NOT NULL,
    PRIMARY KEY (PaperId, KeywordId),
    FOREIGN KEY (PaperId) REFERENCES Papers(PaperId),
    FOREIGN KEY (KeywordId) REFERENCES Keywords(KeywordId)
);


/* =====================================================
   ANALYTICS DOMAIN
===================================================== */

CREATE TABLE PublicationTrends (
    TrendId BIGINT IDENTITY PRIMARY KEY,
    TopicId INT,
    KeywordId INT,
    TrendYear INT,
    PaperCount INT,
    LastUpdated DATETIME2,
    FOREIGN KEY (TopicId) REFERENCES ResearchTopics(TopicId),
    FOREIGN KEY (KeywordId) REFERENCES Keywords(KeywordId)
);

CREATE TABLE TrendSnapshots (
    SnapshotId BIGINT IDENTITY PRIMARY KEY,
    TopicId INT,
    KeywordId INT,
    SnapshotDate DATETIME2,
    TrendScore FLOAT,
    GrowthRate FLOAT,
    FOREIGN KEY (TopicId) REFERENCES ResearchTopics(TopicId),
    FOREIGN KEY (KeywordId) REFERENCES Keywords(KeywordId)
);

CREATE TABLE DashboardReports (
    ReportId BIGINT IDENTITY PRIMARY KEY,
    UserId INT,
    ReportName NVARCHAR(200),
    ReportType NVARCHAR(50),
    FilterConfig NVARCHAR(MAX),
    GeneratedAt DATETIME2,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);


/* =====================================================
   PERSONALIZATION
===================================================== */

CREATE TABLE Bookmarks (
    BookmarkId BIGINT IDENTITY PRIMARY KEY,
    UserId INT,
    TargetId BIGINT,
    TargetType NVARCHAR(50),
    CreatedAt DATETIME2,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE Follows (
    FollowId BIGINT IDENTITY PRIMARY KEY,
    UserId INT,
    TargetId BIGINT,
    TargetType NVARCHAR(50),
    CreatedAt DATETIME2,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);


/* =====================================================
   NOTIFICATIONS
===================================================== */

CREATE TABLE Notifications (
    NotificationId BIGINT IDENTITY PRIMARY KEY,
    UserId INT,
    Message NVARCHAR(MAX),
    RelatedId BIGINT,
    RelatedType NVARCHAR(50),
    IsRead BIT,
    CreatedAt DATETIME2,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);


/* =====================================================
   ADMIN & MONITORING
===================================================== */

CREATE TABLE ActivityLogs (
    LogId BIGINT IDENTITY PRIMARY KEY,
    UserId INT,
    ActionType NVARCHAR(100),
    EntityName NVARCHAR(100),
    EntityId BIGINT,
    CreatedAt DATETIME2,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);


/* =====================================================
   DATA SYNC
===================================================== */

CREATE TABLE SyncJobs (
    SyncJobId BIGINT IDENTITY PRIMARY KEY,
    SourceId INT,
    StartTime DATETIME2,
    EndTime DATETIME2,
    Status NVARCHAR(50),
    RecordsFetched INT,
    ErrorMessage NVARCHAR(MAX),
    FOREIGN KEY (SourceId) REFERENCES ApiDataSources(SourceId)
);


/* =====================================================
   CITATIONS
===================================================== */

CREATE TABLE PaperCitations (
    CitationId BIGINT IDENTITY PRIMARY KEY,
    CitingPaperId BIGINT NOT NULL,
    CitedPaperId BIGINT NOT NULL,
    CreatedAt DATETIME2,
    FOREIGN KEY (CitingPaperId) REFERENCES Papers(PaperId),
    FOREIGN KEY (CitedPaperId) REFERENCES Papers(PaperId)
);


/* =====================================================
   COLLABORATION
===================================================== */

CREATE TABLE ResearchGroups (
    GroupId INT IDENTITY PRIMARY KEY,
    GroupName NVARCHAR(200) NOT NULL,
    OwnerId INT NOT NULL,
    Description NVARCHAR(MAX),
    CreatedAt DATETIME2,
    FOREIGN KEY (OwnerId) REFERENCES Users(UserId)
);

CREATE TABLE GroupMembers (
    GroupId INT NOT NULL,
    UserId INT NOT NULL,
    RoleInGroup NVARCHAR(50),
    JoinedAt DATETIME2,
    PRIMARY KEY (GroupId, UserId),
    FOREIGN KEY (GroupId) REFERENCES ResearchGroups(GroupId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);