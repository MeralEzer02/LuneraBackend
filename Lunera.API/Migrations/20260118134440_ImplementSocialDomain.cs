using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lunera.API.Migrations
{
    /// <inheritdoc />
    public partial class ImplementSocialDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_MatchId",
                table: "Messages",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_UserAId",
                table: "Matches",
                column: "UserAId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_UserBId",
                table: "Matches",
                column: "UserBId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_UserAId",
                table: "Matches",
                column: "UserAId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_UserBId",
                table: "Matches",
                column: "UserBId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Matches_MatchId",
                table: "Messages",
                column: "MatchId",
                principalTable: "Matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_SenderId",
                table: "Messages",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_Users_UserId",
                table: "UserProfiles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_UserAId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_UserBId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Matches_MatchId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_SenderId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_Users_UserId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Messages_MatchId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_SenderId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Matches_UserAId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_UserBId",
                table: "Matches");
        }
    }
}
