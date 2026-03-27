using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeatStore.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LicenseId",
                table: "Orders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Licenses",
                type: "TEXT",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_LicenseId",
                table: "Orders",
                column: "LicenseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Licenses_LicenseId",
                table: "Orders",
                column: "LicenseId",
                principalTable: "Licenses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Licenses_LicenseId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_LicenseId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LicenseId",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Licenses",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
