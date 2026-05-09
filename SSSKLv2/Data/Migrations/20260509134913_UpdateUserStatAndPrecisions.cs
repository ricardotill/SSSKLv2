using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSSKLv2.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserStatAndPrecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserStat_AspNetUsers_UserId",
                table: "UserStat");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserStat",
                table: "UserStat");

            migrationBuilder.RenameTable(
                name: "UserStat",
                newName: "UserStats");

            migrationBuilder.RenameIndex(
                name: "IX_UserStat_UserId",
                table: "UserStats",
                newName: "IX_UserStats_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserStat_Id",
                table: "UserStats",
                newName: "IX_UserStats_Id");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOrderDate",
                table: "UserStats",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTopUpDate",
                table: "UserStats",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxOrdersPerHour",
                table: "UserStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxSingleTopUp",
                table: "UserStats",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "MembershipStartDate",
                table: "UserStats",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinMinutesBetweenOrders",
                table: "UserStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinMinutesBetweenTopUp",
                table: "UserStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuoteVotesReceived",
                table: "UserStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserStats",
                table: "UserStats",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserStats_AspNetUsers_UserId",
                table: "UserStats",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserStats_AspNetUsers_UserId",
                table: "UserStats");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserStats",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "LastOrderDate",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "LastTopUpDate",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "MaxOrdersPerHour",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "MaxSingleTopUp",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "MembershipStartDate",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "MinMinutesBetweenOrders",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "MinMinutesBetweenTopUp",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "QuoteVotesReceived",
                table: "UserStats");

            migrationBuilder.RenameTable(
                name: "UserStats",
                newName: "UserStat");

            migrationBuilder.RenameIndex(
                name: "IX_UserStats_UserId",
                table: "UserStat",
                newName: "IX_UserStat_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserStats_Id",
                table: "UserStat",
                newName: "IX_UserStat_Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserStat",
                table: "UserStat",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserStat_AspNetUsers_UserId",
                table: "UserStat",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
