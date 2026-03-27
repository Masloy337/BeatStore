using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeatStore.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSoldToBeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSold",
                table: "Beats",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSold",
                table: "Beats");
        }
    }
}
