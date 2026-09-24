using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocarDispatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexesForFilteringAndPagination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleType_Department_FirstName_LastName",
                table: "Users",
                columns: new[] { "RoleType", "Department", "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Status_TeamName",
                table: "Teams",
                columns: new[] { "Status", "TeamName" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_CreatedAt_Status_Category",
                table: "Incidents",
                columns: new[] { "CreatedAt", "Status", "Category" },
                descending: new[] { true, false, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_RoleType_Department_FirstName_LastName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Teams_Status_TeamName",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_CreatedAt_Status_Category",
                table: "Incidents");
        }
    }
}
