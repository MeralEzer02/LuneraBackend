using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lunera.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBanSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    IssuedByAdminId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BanUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnbannedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnbannedByAdminId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBans_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBans_Users_IssuedByAdminId",
                        column: x => x.IssuedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBans_Users_UnbannedByAdminId",
                        column: x => x.UnbannedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_IssuedByAdminId",
                table: "UserBans",
                column: "IssuedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_ReportId",
                table: "UserBans",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_UnbannedByAdminId",
                table: "UserBans",
                column: "UnbannedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_UserId",
                table: "UserBans",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBans");
        }
    }
}
