using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCredits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastCreditResetTime",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemainingCredits",
                table: "Users",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCreditResetTime",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RemainingCredits",
                table: "Users");
        }
    }
}
