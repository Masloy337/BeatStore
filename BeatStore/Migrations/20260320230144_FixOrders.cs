using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeatStore.Migrations
{
    /// <inheritdoc />
    public partial class FixOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "Orders",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<int>(
                name: "BeatId",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BeatId",
                table: "Orders",
                column: "BeatId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Beats_BeatId",
                table: "Orders",
                column: "BeatId",
                principalTable: "Beats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Beats_BeatId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BeatId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BeatId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Orders",
                newName: "TotalAmount");

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderDate",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
