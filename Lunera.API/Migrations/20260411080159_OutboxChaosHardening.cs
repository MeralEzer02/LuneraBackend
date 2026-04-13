using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lunera.API.Migrations
{
    /// <inheritdoc />
    public partial class OutboxChaosHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "OutboxMessages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_EventId",
                table: "OutboxMessages",
                column: "EventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_EventId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "OutboxMessages");
        }
    }
}
