using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[ActivityLogs]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityLogs_CreatedAt' AND object_id = OBJECT_ID(N'[ActivityLogs]'))
    CREATE INDEX [IX_ActivityLogs_CreatedAt] ON [ActivityLogs] ([CreatedAt]);
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[ActivityLogs]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityLogs_UserId' AND object_id = OBJECT_ID(N'[ActivityLogs]'))
    CREATE INDEX [IX_ActivityLogs_UserId] ON [ActivityLogs] ([UserId]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");
        }
    }
}
