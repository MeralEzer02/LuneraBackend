using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lunera.API.Migrations
{
    /// <inheritdoc />
    public partial class CooldownUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_UserAId_UserBId",
                table: "Matches");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_UserAId_UserBId",
                table: "Matches",
                columns: new[] { "UserAId", "UserBId" },
                unique: true,
                filter: "[Status] IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_UserAId_UserBId",
                table: "Matches");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_UserAId_UserBId",
                table: "Matches",
                columns: new[] { "UserAId", "UserBId" },
                unique: true,
                filter: "[Status] IN (1, 2)");
        }
    }
}
