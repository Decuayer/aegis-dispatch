using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocarDispatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentMediaRelationalTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "Incidents");

            migrationBuilder.CreateTable(
                name: "Incident_Media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MediaType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incident_Media", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incident_Media_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incident_Media_IncidentId",
                table: "Incident_Media",
                column: "IncidentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incident_Media");

            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "Incidents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
