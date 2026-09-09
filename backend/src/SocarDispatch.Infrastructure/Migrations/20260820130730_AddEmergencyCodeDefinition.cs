using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SocarDispatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmergencyCodeDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Emergency_Codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ColorHex = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    SeverityLevel = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emergency_Codes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Emergency_Codes",
                columns: new[] { "Id", "Code", "ColorHex", "CreatedAt", "Description", "IsActive", "SeverityLevel" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Red", "#FF3B30", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Critical emergency requiring immediate dispatch", true, 1 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Yellow", "#FFCC00", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "High-risk incident requiring prompt response", true, 2 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Green", "#34C759", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Low-risk/informational report", true, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Emergency_Codes_Code",
                table: "Emergency_Codes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Emergency_Codes");
        }
    }
}
