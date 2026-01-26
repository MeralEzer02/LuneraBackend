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
        }
    }
}