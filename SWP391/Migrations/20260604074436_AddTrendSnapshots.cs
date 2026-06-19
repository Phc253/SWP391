using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391.Migrations
{
    /// <inheritdoc />
    public partial class AddTrendSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'DateOfBirth') IS NULL
    ALTER TABLE [Users] ADD [DateOfBirth] date NULL;
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'PhoneNumber') IS NULL
    ALTER TABLE [Users] ADD [PhoneNumber] nvarchar(20) NULL;
");

            migrationBuilder.Sql(@"
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
");

            migrationBuilder.Sql(@"
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
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[EmailVerificationTokens]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmailVerificationTokens_TokenHash' AND object_id = OBJECT_ID(N'[EmailVerificationTokens]'))
    CREATE INDEX [IX_EmailVerificationTokens_TokenHash] ON [EmailVerificationTokens] ([TokenHash]);
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[EmailVerificationTokens]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmailVerificationTokens_User_UsedAt' AND object_id = OBJECT_ID(N'[EmailVerificationTokens]'))
    CREATE INDEX [IX_EmailVerificationTokens_User_UsedAt] ON [EmailVerificationTokens] ([UserId], [UsedAt]);
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[TrendSnapshots]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TrendSnapshots_Date' AND object_id = OBJECT_ID(N'[TrendSnapshots]'))
    CREATE INDEX [IX_TrendSnapshots_Date] ON [TrendSnapshots] ([SnapshotDate]);
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[TrendSnapshots]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TrendSnapshots_Keyword_Date' AND object_id = OBJECT_ID(N'[TrendSnapshots]'))
    CREATE INDEX [IX_TrendSnapshots_Keyword_Date] ON [TrendSnapshots] ([KeywordId], [SnapshotDate]);
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[TrendSnapshots]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TrendSnapshots_Topic_Date' AND object_id = OBJECT_ID(N'[TrendSnapshots]'))
    CREATE INDEX [IX_TrendSnapshots_Topic_Date] ON [TrendSnapshots] ([TopicId], [SnapshotDate]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailVerificationTokens");

            migrationBuilder.DropTable(
                name: "TrendSnapshots");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");
        }
    }
}
