using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TheSocialMediaV2.API.Entities;
using TheSocialMediaV2.API.Events;

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
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- EŞLEŞME (MATCH) KURALLARI ---
            modelBuilder.Entity<Match>(entity =>
            {
                entity.UsePropertyAccessMode(PropertyAccessMode.Field);

                // YENİ: SQL SERVER CHECK CONSTRAINTS (DB SEVİYESİ KALKANLAR)
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Match_UserNormalization", "[UserAId] < [UserBId]");
                    t.HasCheckConstraint("CK_Match_TTL", "[ExpiresAt] > [CreatedAt]");
                });

                entity.HasOne(m => m.UserA)
                    .WithMany(u => u.MatchesAsUserA)
                    .HasForeignKey(m => m.UserAId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.UserB)
                    .WithMany(u => u.MatchesAsUserB)
                    .HasForeignKey(m => m.UserBId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(m => new { m.UserAId, m.UserBId })
                      .IsUnique()
                      .HasFilter("[Status] IN (1, 2)");

                entity.Property(m => m.RowVersion)
                      .IsRowVersion();
            });

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

            // --- USER BAN KURALLARI (IMMUTABLE HISTORY & FINTECH GRADE) ---
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

                entity.Property(b => b.Reason).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
                entity.Property(b => b.BanUntil).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
                entity.Property(b => b.CreatedAt).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
                entity.Property(b => b.ReportId).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
                entity.Property(b => b.IssuedByAdminId).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
                entity.Property(b => b.UserId).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
            });

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

            // --- OUTBOX MESSAGE KURALLARI (HARDENED) ---
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                // SQL SERVER CHECK CONSTRAINTS
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Outbox_RetryCount", "[RetryCount] >= 0");
                });

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Type)
                      .IsRequired()
                      .HasColumnType("nvarchar(max)");

                entity.Property(e => e.Payload)
                      .IsRequired()
                      .HasColumnType("nvarchar(max)");

                entity.Property(e => e.Error)
                      .IsRequired(false)
                      .HasColumnType("nvarchar(max)");

                entity.Property(e => e.RetryCount)
                      .IsRequired()
                      .HasDefaultValue(0);

                entity.HasIndex(e => e.ProcessedOnUtc)
                      .HasFilter("[ProcessedOnUtc] IS NULL");

                entity.HasIndex(e => e.OccurredOnUtc);

                entity.HasIndex(e => e.RetryCount)
                      .HasFilter("[RetryCount] > 0");
            });
        }

        // --- ATOMIC OUTBOX IMPLEMENTATION & INVARIANT GUARD ---
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Invariant Validation
            var matches = ChangeTracker.Entries<Match>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity);

            foreach (var match in matches)
            {
                match.EnsureInvariants();
            }

            ConvertDomainEventsToOutboxMessages();

            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ConvertDomainEventsToOutboxMessages()
        {
            var events = ChangeTracker
                .Entries<IHasDomainEvents>()
                .Select(x => x.Entity)
                .Where(entity => entity.DomainEvents.Any())
                .ToList();

            if (!events.Any()) return;

            var outboxMessages = events
                .SelectMany(entity =>
                {
                    var domainEvents = entity.DomainEvents.ToList();

                    entity.ClearDomainEvents();

                    return domainEvents;
                })
                .Select(domainEvent => new OutboxMessage(
                    id: Guid.NewGuid(),
                    occurredOnUtc: DateTime.UtcNow,
                    type: domainEvent.GetType().AssemblyQualifiedName!,
                    payload: JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    })
                ))
                .ToList();

            OutboxMessages.AddRange(outboxMessages);
        }
    }
}