using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocarDispatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneUniqueAndDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Teams",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Idle");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentLatitude",
                table: "Teams",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentLongitude",
                table: "Teams",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentLatitude",
                table: "Incidents",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentLongitude",
                table: "Incidents",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                table: "Users",
                column: "Phone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Phone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CurrentLatitude",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "CurrentLongitude",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "CurrentLatitude",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "CurrentLongitude",
                table: "Incidents");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Teams",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Idle",
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
