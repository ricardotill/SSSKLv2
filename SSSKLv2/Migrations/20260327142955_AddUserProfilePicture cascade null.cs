using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSSKLv2.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfilePicturecascadenull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_BlobStorageItem_ProfileImageId",
                table: "AspNetUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_BlobStorageItem_ProfileImageId",
                table: "AspNetUsers",
                column: "ProfileImageId",
                principalTable: "BlobStorageItem",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_BlobStorageItem_ProfileImageId",
                table: "AspNetUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_BlobStorageItem_ProfileImageId",
                table: "AspNetUsers",
                column: "ProfileImageId",
                principalTable: "BlobStorageItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
