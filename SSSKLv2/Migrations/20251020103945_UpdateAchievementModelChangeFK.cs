using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSSKLv2.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAchievementModelChangeFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlobStorageItem_Achievement_AchievementId",
                table: "BlobStorageItem");

            migrationBuilder.DropIndex(
                name: "IX_BlobStorageItem_AchievementId",
                table: "BlobStorageItem");

            migrationBuilder.DropColumn(
                name: "AchievementId",
                table: "BlobStorageItem");

            migrationBuilder.AddColumn<Guid>(
                name: "ImageId",
                table: "Achievement",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Achievement_ImageId",
                table: "Achievement",
                column: "ImageId",
                unique: true,
                filter: "[ImageId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Achievement_BlobStorageItem_ImageId",
                table: "Achievement",
                column: "ImageId",
                principalTable: "BlobStorageItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Achievement_BlobStorageItem_ImageId",
                table: "Achievement");

            migrationBuilder.DropIndex(
                name: "IX_Achievement_ImageId",
                table: "Achievement");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "Achievement");

            migrationBuilder.AddColumn<Guid>(
                name: "AchievementId",
                table: "BlobStorageItem",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlobStorageItem_AchievementId",
                table: "BlobStorageItem",
                column: "AchievementId",
                unique: true,
                filter: "[AchievementId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_BlobStorageItem_Achievement_AchievementId",
                table: "BlobStorageItem",
                column: "AchievementId",
                principalTable: "Achievement",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
