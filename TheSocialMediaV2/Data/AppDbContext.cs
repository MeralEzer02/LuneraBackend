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
        public DbSet<ProcessedEvent> ProcessedEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- EŞLEŞME KURALLARI ---
            modelBuilder.Entity<Match>()
                .HasOne(m => m.UserA)
                .WithMany(u => u.MatchesAsUserA)
                .HasForeignKey(m => m.UserAId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.UserB)
                .WithMany(u => u.MatchesAsUserB)
                .HasForeignKey(m => m.UserBId)
                .OnDelete(DeleteBehavior.Restrict);


            // --- MESAJ KURALLARI ---
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- ADMIN ACTION LOG KURALLARI ---
            modelBuilder.Entity<AdminActionLog>(entity =>
            {
                entity.HasOne(l => l.AdminUser)
                    .WithMany(u => u.ActionsAsAdmin)
                    .HasForeignKey(l => l.AdminUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.TargetUser)
                    .WithMany(u => u.ActionsAsTarget)
                    .HasForeignKey(l => l.TargetUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.ActionType)
                    .HasConversion<int>();
            });

            // --- REPORT KURALLARI ---
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasOne(r => r.Reporter)
                    .WithMany()
                    .HasForeignKey(r => r.ReporterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.ReportedUser)
                    .WithMany()
                    .HasForeignKey(r => r.ReportedUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.ProcessedByAdmin)
                    .WithMany()
                    .HasForeignKey(r => r.ProcessedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(r => r.Status)
                    .HasConversion<int>();
            });


            // --- USER BAN KURALLARI (IMMUTABLE HISTORY) ---
            modelBuilder.Entity<UserBan>(entity =>
            {
                entity.HasOne(b => b.User)
                    .WithMany(u => u.BansReceived)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.IssuedByAdmin)
                    .WithMany(u => u.BansIssued)
                    .HasForeignKey(b => b.IssuedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.UnbannedByAdmin)
                    .WithMany()
                    .HasForeignKey(b => b.UnbannedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Report)
                    .WithMany()
                    .HasForeignKey(b => b.ReportId)
                    .OnDelete(DeleteBehavior.Restrict);

                // --- FINTECH GRADE IMMUTABILITY ---
                entity.Property(b => b.Reason).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
                entity.Property(b => b.BanUntil).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
                entity.Property(b => b.CreatedAt).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
                entity.Property(b => b.ReportId).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
                entity.Property(b => b.IssuedByAdminId).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
                entity.Property(b => b.UserId).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
            });

            // DB Level Determinism
            modelBuilder.Entity<UserBan>()
                .HasIndex(b => b.UserId)
                .IsUnique()
                .HasFilter("[UnbannedAt] IS NULL");

            // --- USER ABUSE METRIC AYARLARI ---
            modelBuilder.Entity<UserAbuseMetric>(entity =>
            {
                entity.HasKey(m => m.UserId);
                entity.HasOne(m => m.User)
                    .WithOne()
                    .HasForeignKey<UserAbuseMetric>(m => m.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}