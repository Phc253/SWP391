USE ScientificTrendDB;
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        UserId INT IDENTITY PRIMARY KEY,
        Email NVARCHAR(255) NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        FullName NVARCHAR(150) NULL,
        DateOfBirth DATE NULL,
        PhoneNumber NVARCHAR(20) NULL,
        ActorType NVARCHAR(50) NOT NULL DEFAULT N'Student',
        CreatedAt DATETIME2 DEFAULT SYSDATETIME(),
        IsActive BIT DEFAULT 1
    );
END
GO

IF COL_LENGTH('dbo.Users', 'FullName') IS NULL
    ALTER TABLE dbo.Users ADD FullName NVARCHAR(150) NULL;
GO

IF COL_LENGTH('dbo.Users', 'DateOfBirth') IS NULL
    ALTER TABLE dbo.Users ADD DateOfBirth DATE NULL;
GO

IF COL_LENGTH('dbo.Users', 'PhoneNumber') IS NULL
    ALTER TABLE dbo.Users ADD PhoneNumber NVARCHAR(20) NULL;
GO

IF COL_LENGTH('dbo.Users', 'ActorType') IS NULL
    ALTER TABLE dbo.Users
    ADD ActorType NVARCHAR(50) NOT NULL
        CONSTRAINT DF_Users_ActorType DEFAULT N'Student';
GO

IF COL_LENGTH('dbo.Users', 'CreatedAt') IS NULL
    ALTER TABLE dbo.Users ADD CreatedAt DATETIME2 NULL;
GO

IF COL_LENGTH('dbo.Users', 'IsActive') IS NULL
    ALTER TABLE dbo.Users ADD IsActive BIT NULL;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Users')
      AND c.name = N'CreatedAt'
)
    ALTER TABLE dbo.Users
    ADD CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME() FOR CreatedAt;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Users')
      AND c.name = N'IsActive'
)
    ALTER TABLE dbo.Users
    ADD CONSTRAINT DF_Users_IsActive DEFAULT 1 FOR IsActive;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic
        ON ic.object_id = i.object_id
       AND ic.index_id = i.index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id
       AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.Users')
      AND i.is_unique = 1
      AND c.name = N'Email'
)
    CREATE UNIQUE INDEX UX_Users_Email
    ON dbo.Users(Email);
GO

IF OBJECT_ID('dbo.EmailVerificationTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmailVerificationTokens (
        EmailVerificationTokenId BIGINT IDENTITY PRIMARY KEY,
        UserId INT NOT NULL,
        TokenHash NVARCHAR(255) NOT NULL,
        ExpiresAt DATETIME2 NOT NULL,
        CreatedAt DATETIME2 DEFAULT SYSDATETIME(),
        UsedAt DATETIME2 NULL,

        CONSTRAINT FK_EmailVerificationTokens_Users
            FOREIGN KEY (UserId)
            REFERENCES dbo.Users(UserId)
            ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_EmailVerificationTokens_TokenHash'
      AND object_id = OBJECT_ID(N'dbo.EmailVerificationTokens')
)
    CREATE INDEX IX_EmailVerificationTokens_TokenHash
    ON dbo.EmailVerificationTokens(TokenHash);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_EmailVerificationTokens_User_UsedAt'
      AND object_id = OBJECT_ID(N'dbo.EmailVerificationTokens')
)
    CREATE INDEX IX_EmailVerificationTokens_User_UsedAt
    ON dbo.EmailVerificationTokens(UserId, UsedAt);
GO
