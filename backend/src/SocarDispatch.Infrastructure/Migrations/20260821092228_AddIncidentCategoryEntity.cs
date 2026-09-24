using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SocarDispatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentCategoryEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incident_Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incident_Categories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Incident_Categories",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("a1111111-1111-1111-1111-111111111111"), "Fire", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Fire and combustion incidents", true, "Fire Emergency" },
                    { new Guid("a2222222-2222-2222-2222-222222222222"), "Medical", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Medical emergencies and injuries", true, "Medical Emergency" },
                    { new Guid("a3333333-3333-3333-3333-333333333333"), "Security", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Physical and facility security incidents", true, "Security Incident" },
                    { new Guid("a4444444-4444-4444-4444-444444444444"), "Environmental", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Environmental contamination and spills", true, "Environmental Hazard" },
                    { new Guid("a5555555-5555-5555-5555-555555555555"), "Chemical", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Chemical leaks and toxic substance exposure", true, "Chemical Incident" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incident_Categories_Code",
                table: "Incident_Categories",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incident_Categories");
        }
    }
}
