using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Bookmarks__UserI__4F7CD00D",
                table: "Bookmarks");

            migrationBuilder.DropForeignKey(
                name: "FK__Follows__UserId__534D60F1",
                table: "Follows");

            migrationBuilder.DropForeignKey(
                name: "FK__Notificat__UserI__5812160E",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK__PaperAuth__Autho__3C69FB99",
                table: "PaperAuthors");

            migrationBuilder.DropForeignKey(
                name: "FK__PaperAuth__Paper__3B75D760",
                table: "PaperAuthors");

            migrationBuilder.DropIndex(
                name: "UQ__Research__6C795E8C662E12F0",
                table: "ResearchTopics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaperAuthors",
                table: "PaperAuthors");

            migrationBuilder.DropIndex(
                name: "UQ__Keywords__219EE3D704701796",
                table: "Keywords");

            migrationBuilder.AlterColumn<int>(
                name: "SourceId",
                table: "SyncJobs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "TopicName",
                table: "ResearchTopics",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<int>(
                name: "TrendYear",
                table: "PublicationTrends",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PaperCount",
                table: "PublicationTrends",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Papers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Affiliation",
                table: "PaperAuthors",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthorOrder",
                table: "PaperAuthors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCorresponding",
                table: "PaperAuthors",
                type: "bit",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Notifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "KeywordText",
                table: "Keywords",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "JournalName",
                table: "Journals",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Journals",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ImpactFactor",
                table: "Journals",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Journals",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Follows",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "TargetType",
                table: "Follows",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<long>(
                name: "TargetId",
                table: "Follows",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Bookmarks",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "TargetType",
                table: "Bookmarks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<long>(
                name: "TargetId",
                table: "Bookmarks",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "AuthorName",
                table: "Authors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "ResearchArea",
                table: "Authors",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalPublications",
                table: "Authors",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceName",
                table: "ApiDataSources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddPrimaryKey(
                name: "PK__PaperAut__FC8BBDC843D72F9A",
                table: "PaperAuthors",
                columns: new[] { "PaperId", "AuthorId" });

            migrationBuilder.CreateTable(
                name: "DashboardReports",
                columns: table => new
                {
                    ReportId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ReportName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FilterConfig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Dashboar__D5BD48054FB0E0D0", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK__Dashboard__UserI__5070F446",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "PaperCitations",
                columns: table => new
                {
                    CitationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CitingPaperId = table.Column<long>(type: "bigint", nullable: false),
                    CitedPaperId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PaperCit__EAD2ADFB7C803DC4", x => x.CitationId);
                    table.ForeignKey(
                        name: "FK__PaperCita__Cited__628FA481",
                        column: x => x.CitedPaperId,
                        principalTable: "Papers",
                        principalColumn: "PaperId");
                    table.ForeignKey(
                        name: "FK__PaperCita__Citin__619B8048",
                        column: x => x.CitingPaperId,
                        principalTable: "Papers",
                        principalColumn: "PaperId");
                });

            migrationBuilder.CreateTable(
                name: "ResearchGroups",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Research__149AF36A1F2F87A7", x => x.GroupId);
                    table.ForeignKey(
                        name: "FK__ResearchG__Owner__656C112C",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    PreferenceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    PreferredField = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PreferredYearRange = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NotificationFrequency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserPref__E228496F80308D5A", x => x.PreferenceId);
                    table.ForeignKey(
                        name: "FK__UserPrefe__UserI__2E1BDC42",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleInGroup = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__GroupMem__C5E27FAE44EE6647", x => new { x.GroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK__GroupMemb__Group__68487DD7",
                        column: x => x.GroupId,
                        principalTable: "ResearchGroups",
                        principalColumn: "GroupId");
                    table.ForeignKey(
                        name: "FK__GroupMemb__UserI__693CA210",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "UQ__Research__6C795E8C662E12F0",
                table: "ResearchTopics",
                column: "TopicName",
                unique: true,
                filter: "[TopicName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ__Keywords__219EE3D704701796",
                table: "Keywords",
                column: "KeywordText",
                unique: true,
                filter: "[KeywordText] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardReports_UserId",
                table: "DashboardReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_UserId",
                table: "GroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaperCitations_CitedPaperId",
                table: "PaperCitations",
                column: "CitedPaperId");

            migrationBuilder.CreateIndex(
                name: "IX_PaperCitations_CitingPaperId",
                table: "PaperCitations",
                column: "CitingPaperId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchGroups_OwnerId",
                table: "ResearchGroups",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__Bookmarks__UserI__4F7CD00D",
                table: "Bookmarks",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__Follows__UserId__534D60F1",
                table: "Follows",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__Notificat__UserI__5812160E",
                table: "Notifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__PaperAuth__Autho__3C69FB99",
                table: "PaperAuthors",
                column: "AuthorId",
                principalTable: "Authors",
                principalColumn: "AuthorId");

            migrationBuilder.AddForeignKey(
                name: "FK__PaperAuth__Paper__3B75D760",
                table: "PaperAuthors",
                column: "PaperId",
                principalTable: "Papers",
                principalColumn: "PaperId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Bookmarks__UserI__4F7CD00D",
                table: "Bookmarks");

            migrationBuilder.DropForeignKey(
                name: "FK__Follows__UserId__534D60F1",
                table: "Follows");

            migrationBuilder.DropForeignKey(
                name: "FK__Notificat__UserI__5812160E",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK__PaperAuth__Autho__3C69FB99",
                table: "PaperAuthors");

            migrationBuilder.DropForeignKey(
                name: "FK__PaperAuth__Paper__3B75D760",
                table: "PaperAuthors");

            migrationBuilder.DropTable(
                name: "DashboardReports");

            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "PaperCitations");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "ResearchGroups");

            migrationBuilder.DropIndex(
                name: "UQ__Research__6C795E8C662E12F0",
                table: "ResearchTopics");

            migrationBuilder.DropPrimaryKey(
                name: "PK__PaperAut__FC8BBDC843D72F9A",
                table: "PaperAuthors");

            migrationBuilder.DropIndex(
                name: "UQ__Keywords__219EE3D704701796",
                table: "Keywords");

            migrationBuilder.DropColumn(
                name: "Affiliation",
                table: "PaperAuthors");

            migrationBuilder.DropColumn(
                name: "AuthorOrder",
                table: "PaperAuthors");

            migrationBuilder.DropColumn(
                name: "IsCorresponding",
                table: "PaperAuthors");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Journals");

            migrationBuilder.DropColumn(
                name: "ImpactFactor",
                table: "Journals");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Journals");

            migrationBuilder.DropColumn(
                name: "ResearchArea",
                table: "Authors");

            migrationBuilder.DropColumn(
                name: "TotalPublications",
                table: "Authors");

            migrationBuilder.AlterColumn<int>(
                name: "SourceId",
                table: "SyncJobs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TopicName",
                table: "ResearchTopics",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TrendYear",
                table: "PublicationTrends",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PaperCount",
                table: "PublicationTrends",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Papers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "KeywordText",
                table: "Keywords",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JournalName",
                table: "Journals",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Follows",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TargetType",
                table: "Follows",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TargetId",
                table: "Follows",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Bookmarks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TargetType",
                table: "Bookmarks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TargetId",
                table: "Bookmarks",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AuthorName",
                table: "Authors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceName",
                table: "ApiDataSources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaperAuthors",
                table: "PaperAuthors",
                columns: new[] { "PaperId", "AuthorId" });

            migrationBuilder.CreateIndex(
                name: "UQ__Research__6C795E8C662E12F0",
                table: "ResearchTopics",
                column: "TopicName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Keywords__219EE3D704701796",
                table: "Keywords",
                column: "KeywordText",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK__Bookmarks__UserI__4F7CD00D",
                table: "Bookmarks",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__Follows__UserId__534D60F1",
                table: "Follows",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__Notificat__UserI__5812160E",
                table: "Notifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__PaperAuth__Autho__3C69FB99",
                table: "PaperAuthors",
                column: "AuthorId",
                principalTable: "Authors",
                principalColumn: "AuthorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__PaperAuth__Paper__3B75D760",
                table: "PaperAuthors",
                column: "PaperId",
                principalTable: "Papers",
                principalColumn: "PaperId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
