using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lunera.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminActionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AdminId",
                table: "AdminActionLogs",
                newName: "AdminUserId");

            migrationBuilder.AlterColumn<int>(
                name: "TargetUserId",
                table: "AdminActionLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ActionType",
                table: "AdminActionLogs",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "AdminActionLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "AdminActionLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TargetEntityId",
                table: "AdminActionLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetEntityType",
                table: "AdminActionLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminActionLogs_AdminUserId",
                table: "AdminActionLogs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminActionLogs_TargetUserId",
                table: "AdminActionLogs",
                column: "TargetUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminActionLogs_Users_AdminUserId",
                table: "AdminActionLogs",
                column: "AdminUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AdminActionLogs_Users_TargetUserId",
                table: "AdminActionLogs",
                column: "TargetUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminActionLogs_Users_AdminUserId",
                table: "AdminActionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AdminActionLogs_Users_TargetUserId",
                table: "AdminActionLogs");

            migrationBuilder.DropIndex(
                name: "IX_AdminActionLogs_AdminUserId",
                table: "AdminActionLogs");

            migrationBuilder.DropIndex(
                name: "IX_AdminActionLogs_TargetUserId",
                table: "AdminActionLogs");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "AdminActionLogs");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "AdminActionLogs");

            migrationBuilder.DropColumn(
                name: "TargetEntityId",
                table: "AdminActionLogs");

            migrationBuilder.DropColumn(
                name: "TargetEntityType",
                table: "AdminActionLogs");

            migrationBuilder.RenameColumn(
                name: "AdminUserId",
                table: "AdminActionLogs",
                newName: "AdminId");

            migrationBuilder.AlterColumn<int>(
                name: "TargetUserId",
                table: "AdminActionLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActionType",
                table: "AdminActionLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
