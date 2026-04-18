using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSSKLv2.Migrations
{
    /// <inheritdoc />
    public partial class ChangeEventRolesToManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventRequiredRoles",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRequiredRoles", x => new { x.EventId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_EventRequiredRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventRequiredRoles_Event_EventId",
                        column: x => x.EventId,
                        principalTable: "Event",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("INSERT INTO EventRequiredRoles (EventId, RoleId) SELECT EventId, Id FROM AspNetRoles WHERE EventId IS NOT NULL");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoles_Event_EventId",
                table: "AspNetRoles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetRoles_EventId",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "AspNetRoles");

            migrationBuilder.CreateIndex(
                name: "IX_EventRequiredRoles_RoleId",
                table: "EventRequiredRoles",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "AspNetRoles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("UPDATE AspNetRoles SET EventId = (SELECT TOP 1 EventRequiredRoles.EventId FROM EventRequiredRoles WHERE EventRequiredRoles.RoleId = AspNetRoles.Id)");

            migrationBuilder.DropTable(
                name: "EventRequiredRoles");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_EventId",
                table: "AspNetRoles",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoles_Event_EventId",
                table: "AspNetRoles",
                column: "EventId",
                principalTable: "Event",
                principalColumn: "Id");
        }
    }
}
