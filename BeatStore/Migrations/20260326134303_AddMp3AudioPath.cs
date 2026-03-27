using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeatStore.Migrations
{
    /// <inheritdoc />
    public partial class AddMp3AudioPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Mp3AudioPatch",
                table: "Beats",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mp3AudioPatch",
                table: "Beats");
        }
    }
}
