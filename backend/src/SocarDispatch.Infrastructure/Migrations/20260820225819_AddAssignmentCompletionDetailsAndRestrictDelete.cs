using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocarDispatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentCompletionDetailsAndRestrictDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Incidents_IncidentId",
                table: "Assignments");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Teams",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Idle",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "CompletionNotes",
                table: "Assignments",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Incidents_IncidentId",
                table: "Assignments",
                column: "IncidentId",
                principalTable: "Incidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Incidents_IncidentId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CompletionNotes",
                table: "Assignments");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Teams",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Idle");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Incidents_IncidentId",
                table: "Assignments",
                column: "IncidentId",
                principalTable: "Incidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
