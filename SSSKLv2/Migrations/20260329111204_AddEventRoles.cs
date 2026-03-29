using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSSKLv2.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "AspNetRoles",
                type: "uniqueidentifier",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoles_Event_EventId",
                table: "AspNetRoles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetRoles_EventId",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "AspNetRoles");
        }
    }
}
