using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lunera.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDbCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Outbox_RetryCount",
                table: "OutboxMessages",
                sql: "[RetryCount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Match_TTL",
                table: "Matches",
                sql: "[ExpiresAt] > [CreatedAt]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Match_UserNormalization",
                table: "Matches",
                sql: "[UserAId] < [UserBId]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Outbox_RetryCount",
                table: "OutboxMessages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Match_TTL",
                table: "Matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Match_UserNormalization",
                table: "Matches");
        }
    }
}
