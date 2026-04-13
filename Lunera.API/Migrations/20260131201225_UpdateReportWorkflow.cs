using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lunera.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReportWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReporterUserId",
                table: "Reports",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "MatchId",
                table: "Reports",
                newName: "ReporterId");

            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "Reports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessedByAdminId",
                table: "Reports",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ProcessedByAdminId",
                table: "Reports",
                column: "ProcessedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedUserId",
                table: "Reports",
                column: "ReportedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId",
                table: "Reports",
                column: "ReporterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Users_ProcessedByAdminId",
                table: "Reports",
                column: "ProcessedByAdminId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Users_ReportedUserId",
                table: "Reports",
                column: "ReportedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Users_ReporterId",
                table: "Reports",
                column: "ReporterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Users_ProcessedByAdminId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Users_ReportedUserId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Users_ReporterId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ProcessedByAdminId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReportedUserId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ProcessedByAdminId",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Reports",
                newName: "ReporterUserId");

            migrationBuilder.RenameColumn(
                name: "ReporterId",
                table: "Reports",
                newName: "MatchId");
        }
    }
}
