using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lunera.API.Migrations
{
    /// <inheritdoc />
    public partial class CreateMatchDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_UserAId",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "EndedAt",
                table: "Matches",
                newName: "RespondedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Matches",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "RequesterId",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Matches",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_UserAId_UserBId",
                table: "Matches",
                columns: new[] { "UserAId", "UserBId" },
                unique: true,
                filter: "[Status] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_UserAId_UserBId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "RequesterId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "RespondedAt",
                table: "Matches",
                newName: "EndedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_UserAId",
                table: "Matches",
                column: "UserAId");
        }
    }
}
