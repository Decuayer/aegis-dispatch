using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocarDispatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceTokenToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceToken",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeviceTokenUpdatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeviceTokenUpdatedAt",
                table: "Users");
        }
    }
}
