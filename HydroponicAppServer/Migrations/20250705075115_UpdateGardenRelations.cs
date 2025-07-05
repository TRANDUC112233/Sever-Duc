using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HydroponicAppServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGardenRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GardenId",
                table: "SensorDatas",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GardenId",
                table: "DeviceActions",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Gardens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VegetableType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gardens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gardens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SensorDatas_GardenId",
                table: "SensorDatas",
                column: "GardenId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceActions_GardenId",
                table: "DeviceActions",
                column: "GardenId");

            migrationBuilder.CreateIndex(
                name: "IX_Gardens_UserId",
                table: "Gardens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceActions_Gardens_GardenId",
                table: "DeviceActions",
                column: "GardenId",
                principalTable: "Gardens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SensorDatas_Gardens_GardenId",
                table: "SensorDatas",
                column: "GardenId",
                principalTable: "Gardens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceActions_Gardens_GardenId",
                table: "DeviceActions");

            migrationBuilder.DropForeignKey(
                name: "FK_SensorDatas_Gardens_GardenId",
                table: "SensorDatas");

            migrationBuilder.DropTable(
                name: "Gardens");

            migrationBuilder.DropIndex(
                name: "IX_SensorDatas_GardenId",
                table: "SensorDatas");

            migrationBuilder.DropIndex(
                name: "IX_DeviceActions_GardenId",
                table: "DeviceActions");

            migrationBuilder.DropColumn(
                name: "GardenId",
                table: "SensorDatas");

            migrationBuilder.DropColumn(
                name: "GardenId",
                table: "DeviceActions");
        }
    }
}
