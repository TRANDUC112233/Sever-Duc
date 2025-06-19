using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HydroponicAppServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelToMatchChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsScheduled",
                table: "DeviceActions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledTime",
                table: "DeviceActions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DeviceActions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsScheduled",
                table: "DeviceActions");

            migrationBuilder.DropColumn(
                name: "ScheduledTime",
                table: "DeviceActions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DeviceActions");
        }
    }
}
