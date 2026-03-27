using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeatStore.Migrations
{
    /// <inheritdoc />
    public partial class AddProducerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProducerName",
                table: "Beats",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProducerName",
                table: "Beats");
        }
    }
}
