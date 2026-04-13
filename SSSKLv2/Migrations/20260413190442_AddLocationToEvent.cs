using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSSKLv2.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Event",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "Event",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Event",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Event");
        }
    }
}
