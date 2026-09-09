using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocarDispatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixIncidentLatitudeLongitudePrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLatitude",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "CurrentLongitude",
                table: "Incidents");

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Incidents",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Incidents",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Incidents");

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
        }
    }
}
