using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheSocialMediaV2.API.Migrations
{
    public partial class AddUserBanImmutableTrigger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            var createTriggerSql = @"
                CREATE TRIGGER TRG_UserBans_Immutable
                ON UserBans
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Eğer kritik alanlardan biri değişmişse
                    IF UPDATE(Reason) OR UPDATE(BanUntil) OR UPDATE(UserId) OR UPDATE(IssuedByAdminId) OR UPDATE(ReportId)
                    BEGIN
                        RAISERROR ('Güvenlik İhlali: UserBan tablosundaki kritik alanlar (Reason, BanUntil, UserId, ReportId) değiştirilemez! Bu işlem immutable ilkesine aykırıdır.', 16, 1);
                        ROLLBACK TRANSACTION;
                        RETURN;
                    END
                END
            ";

            migrationBuilder.Sql(createTriggerSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_UserBans_Immutable");
        }
    }
}