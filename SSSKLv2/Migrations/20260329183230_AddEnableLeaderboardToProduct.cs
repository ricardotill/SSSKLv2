using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSSKLv2.Migrations
{
    /// <inheritdoc />
    public partial class AddEnableLeaderboardToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableLeaderboard",
                table: "Product",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableLeaderboard",
                table: "Product");
        }
    }
}
