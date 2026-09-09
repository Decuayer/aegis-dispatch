using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocarDispatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incident_Reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    MediaUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incident_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incident_Reports_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Incident_Reports_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incident_Reports_Users_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incident_Reports_IncidentId",
                table: "Incident_Reports",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_Reports_ReportedByUserId",
                table: "Incident_Reports",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_Reports_TeamId",
                table: "Incident_Reports",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incident_Reports");
        }
    }
}
