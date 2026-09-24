using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocarDispatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamMemberIndividualStatusTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedAt",
                table: "Team_Members",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<string>(
                name: "MemberStatus",
                table: "Team_Members",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Available");

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusUpdatedAt",
                table: "Team_Members",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JoinedAt",
                table: "Team_Members");

            migrationBuilder.DropColumn(
                name: "MemberStatus",
                table: "Team_Members");

            migrationBuilder.DropColumn(
                name: "StatusUpdatedAt",
                table: "Team_Members");
        }
    }
}
