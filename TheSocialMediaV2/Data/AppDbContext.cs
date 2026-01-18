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
        }
    }
}