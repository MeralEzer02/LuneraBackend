using Microsoft.EntityFrameworkCore;
using TheSocialMediaV2.API.Entities;

namespace TheSocialMediaV2.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<AdminActionLog> AdminActionLogs { get; set; }
        public DbSet<UserBan> UserBans { get; set; }
        public DbSet<UserAbuseMetric> UserAbuseMetrics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- EŞLEŞME KURALLARI ---

            // UserA silinirse, Match tablosu etkilenmesin
            modelBuilder.Entity<Match>()
                .HasOne(m => m.UserA)
                .WithMany(u => u.MatchesAsUserA)
                .HasForeignKey(m => m.UserAId)
                .OnDelete(DeleteBehavior.Restrict);

            // UserB silinirse, Match tablosu etkilenmesin
            modelBuilder.Entity<Match>()
                .HasOne(m => m.UserB)
                .WithMany(u => u.MatchesAsUserB)
                .HasForeignKey(m => m.UserBId)
                .OnDelete(DeleteBehavior.Restrict);


            // --- MESAJ KURALLARI ---

            // Gönderen (Sender) silinirse, mesajlar silinmesin (Sohbet geçmişi kalsın)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- ADMIN ACTION LOG KURALLARI (KARA KUTU) ---

            modelBuilder.Entity<AdminActionLog>(entity =>
            {
                // 1. Admin silinirse LOG SİLİNMEZ (Delil karartılamaz)
                entity.HasOne(l => l.AdminUser)
                    .WithMany(u => u.ActionsAsAdmin)
                    .HasForeignKey(l => l.AdminUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 2. Hedef Kullanıcı silinirse LOG SİLİNMEZ
                entity.HasOne(l => l.TargetUser)
                    .WithMany(u => u.ActionsAsTarget)
                    .HasForeignKey(l => l.TargetUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 3. Enum dönüşümü
                entity.Property(e => e.ActionType)
                    .HasConversion<int>();
            });

            // --- REPORT KURALLARI ---
            modelBuilder.Entity<Report>(entity =>
            {
                // 1. Şikayet Eden Silinirse Rapor Silinmesin (Delil kalsın)
                entity.HasOne(r => r.Reporter)
                    .WithMany()
                    .HasForeignKey(r => r.ReporterId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 2. Şikayet Edilen Silinirse Rapor Silinmesin
                entity.HasOne(r => r.ReportedUser)
                    .WithMany()
                    .HasForeignKey(r => r.ReportedUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 3. Karar Veren Admin Silinirse Rapor Silinmesin
                entity.HasOne(r => r.ProcessedByAdmin)
                    .WithMany()
                    .HasForeignKey(r => r.ProcessedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 4. Enum Dönüşümü
                entity.Property(r => r.Status)
                    .HasConversion<int>();
            });


            // --- USER BAN KURALLARI (IMMUTABLE HISTORY) ---
            modelBuilder.Entity<UserBan>(entity =>
            {
                // 1. Cezalı Kullanıcı Silinirse Ban Kaydı SİLİNMEZ
                entity.HasOne(b => b.User)
                    .WithMany(u => u.BansReceived)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 2. Banı Atan Admin Silinirse Ban Kaydı SİLİNMEZ
                entity.HasOne(b => b.IssuedByAdmin)
                    .WithMany(u => u.BansIssued)
                    .HasForeignKey(b => b.IssuedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 3. Banı Kaldıran Admin Silinirse Kayıt SİLİNMEZ
                entity.HasOne(b => b.UnbannedByAdmin)
                    .WithMany()
                    .HasForeignKey(b => b.UnbannedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 4. Dayanak Rapor Silinirse Ban Kaydı SİLİNMEZ
                entity.HasOne(b => b.Report)
                    .WithMany()
                    .HasForeignKey(b => b.ReportId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserBan>()
                .HasIndex(b => b.UserId)
                .IsUnique()
                .HasFilter("[UnbannedAt] IS NULL");

            // A3.4 - USER ABUSE METRIC AYARLARI
            modelBuilder.Entity<UserAbuseMetric>(entity =>
            {
                entity.HasKey(m => m.UserId);

                // User silinirse istihbarat dosyası silinmez! (Restrict)
                entity.HasOne(m => m.User)
                    .WithOne()
                    .HasForeignKey<UserAbuseMetric>(m => m.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}