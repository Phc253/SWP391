USE ScientificTrendDB;
GO

IF COL_LENGTH('dbo.Users', 'DateOfBirth') IS NULL
    ALTER TABLE dbo.Users ADD DateOfBirth DATE NULL;
GO

IF COL_LENGTH('dbo.Users', 'PhoneNumber') IS NULL
    ALTER TABLE dbo.Users ADD PhoneNumber NVARCHAR(20) NULL;
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
