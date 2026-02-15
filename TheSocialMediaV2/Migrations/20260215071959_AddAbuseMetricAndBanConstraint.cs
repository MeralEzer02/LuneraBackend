using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheSocialMediaV2.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAbuseMetricAndBanConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserBans_UserId",
                table: "UserBans");

            migrationBuilder.CreateTable(
                name: "UserAbuseMetrics",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TotalReportsReceived = table.Column<int>(type: "int", nullable: false),
                    TotalReportsConfirmed = table.Column<int>(type: "int", nullable: false),
                    TotalReportsRejected = table.Column<int>(type: "int", nullable: false),
                    TotalWarnings = table.Column<int>(type: "int", nullable: false),
                    TotalBans = table.Column<int>(type: "int", nullable: false),
                    LastBanDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AbuseScore = table.Column<int>(type: "int", nullable: false),
                    RiskLevel = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAbuseMetrics", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserAbuseMetrics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_UserId",
                table: "UserBans",
                column: "UserId",
                unique: true,
                filter: "[UnbannedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAbuseMetrics");

            migrationBuilder.DropIndex(
                name: "IX_UserBans_UserId",
                table: "UserBans");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_UserId",
                table: "UserBans",
                column: "UserId");
        }
    }
}
