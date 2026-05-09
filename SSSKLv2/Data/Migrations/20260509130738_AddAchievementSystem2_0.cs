using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSSKLv2.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievementSystem2_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "AchievementEntry",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentAchievementId",
                table: "Achievement",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "Achievement",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UserStat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TotalSpent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalOrders = table.Column<int>(type: "int", nullable: false),
                    TotalItemsBought = table.Column<int>(type: "int", nullable: false),
                    TotalTopUp = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuoteCount = table.Column<int>(type: "int", nullable: false),
                    QuoteVotesGiven = table.Column<int>(type: "int", nullable: false),
                    ReactionCount = table.Column<int>(type: "int", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentStreak = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStat_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Achievement_ParentAchievementId",
                table: "Achievement",
                column: "ParentAchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStat_Id",
                table: "UserStat",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserStat_UserId",
                table: "UserStat",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Achievement_Achievement_ParentAchievementId",
                table: "Achievement",
                column: "ParentAchievementId",
                principalTable: "Achievement",
                principalColumn: "Id");

            // Backfill UserStat table
            migrationBuilder.Sql(@"
                INSERT INTO UserStat (Id, UserId, CreatedOn, TotalSpent, TotalOrders, TotalItemsBought, TotalTopUp, QuoteCount, QuoteVotesGiven, ReactionCount, CurrentStreak)
                SELECT 
                    NEWID(), 
                    u.Id, 
                    GETDATE(), 
                    COALESCE((SELECT SUM(o.Paid) FROM [Order] o WHERE o.UserId = u.Id), 0),
                    COALESCE((SELECT COUNT(*) FROM [Order] o WHERE o.UserId = u.Id), 0),
                    COALESCE((SELECT SUM(o.Amount) FROM [Order] o WHERE o.UserId = u.Id), 0),
                    COALESCE((SELECT SUM(t.Saldo) FROM TopUp t WHERE t.UserId = u.Id), 0),
                    COALESCE((SELECT COUNT(*) FROM Quote q WHERE q.CreatedById = u.Id), 0),
                    COALESCE((SELECT COUNT(*) FROM QuoteVote qv WHERE qv.UserId = u.Id), 0),
                    COALESCE((SELECT COUNT(*) FROM Reaction r WHERE r.UserId = u.Id), 0),
                    0
                FROM AspNetUsers u
                WHERE NOT EXISTS (SELECT 1 FROM UserStat us WHERE us.UserId = u.Id)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Achievement_Achievement_ParentAchievementId",
                table: "Achievement");

            migrationBuilder.DropTable(
                name: "UserStat");

            migrationBuilder.DropIndex(
                name: "IX_Achievement_ParentAchievementId",
                table: "Achievement");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "AchievementEntry");

            migrationBuilder.DropColumn(
                name: "ParentAchievementId",
                table: "Achievement");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Achievement");
        }
    }
}
