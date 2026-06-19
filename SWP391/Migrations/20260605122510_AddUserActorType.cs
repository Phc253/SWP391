using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391.Migrations
{
    /// <inheritdoc />
    public partial class AddUserActorType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'ActorType') IS NULL
    ALTER TABLE [Users] ADD [ActorType] nvarchar(50) NOT NULL CONSTRAINT [DF_Users_ActorType] DEFAULT N'Student';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorType",
                table: "Users");
        }
    }
}
