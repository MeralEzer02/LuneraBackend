using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TheSocialMediaV2.API.Events;

namespace TheSocialMediaV2.API.Entities
{
    // Eşleşme Durumları
    public enum MatchStatus
    {
        Pending = 1,
        Accepted = 2,
        Rejected = 3,
        Cancelled = 4,
        Expired = 5
    }

    public class Match : IHasDomainEvents
    {
        // --- DOMAIN EVENTS ---
        private readonly List<IInternalDomainEvent> _domainEvents = new();

        [NotMapped]
        public IReadOnlyCollection<IInternalDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void ClearDomainEvents() => _domainEvents.Clear();

        private void AddDomainEvent(IInternalDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }
        // ----------------------------------------------

        protected Match() { }

        // Private Constructor
        private Match(int initiatorId, int targetId, int durationHours)
        {
            // ID Normalization
            if (initiatorId < targetId)
            {
                UserAId = initiatorId;
                UserBId = targetId;
            }
            else
            {
                UserAId = targetId;
                UserBId = initiatorId;
            }

            RequesterId = initiatorId;
            Status = MatchStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = DateTime.UtcNow.AddHours(durationHours);
        }

        public int Id { get; private set; }

        // --- DOMAIN STATE PROPERTIES ---

        [Required]
        public int UserAId { get; private set; }

        [ForeignKey("UserAId")]
        public virtual User UserA { get; private set; }

        [Required]
        public int UserBId { get; private set; }

        [ForeignKey("UserBId")]
        public virtual User UserB { get; private set; }

        [Required]
        public int RequesterId { get; private set; }

        [Required]
        public MatchStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime ExpiresAt { get; private set; }

        public DateTime? RespondedAt { get; private set; }

        // --- OPTIMISTIC CONCURRENCY CONTROL ---
        [Timestamp]
        public byte[] RowVersion { get; private set; }

        // --- FACTORY METHOD (Tek Giriş Kapısı) ---
        public static Match Create(int initiatorId, int targetId, int durationHours = 24)
        {
            if (initiatorId == targetId)
                throw new InvalidOperationException("Kullanıcı kendisiyle eşleşemez.");

            if (durationHours <= 0)
                throw new ArgumentException("Süre 0'dan büyük olmalıdır.");

            var match = new Match(initiatorId, targetId, durationHours);

            // EVENT GENERATION: MatchCreated
            match.AddDomainEvent(new MatchCreatedEvent(0, initiatorId, targetId));

            return match;
        }

        // --- STATE TRANSITION METHODS (DAVRANIŞLAR & EVENTS) ---

        // 1. KABUL ETME
        public void Accept()
        {
            if (Status != MatchStatus.Pending)
                throw new InvalidOperationException($"Match kabul edilemez. Mevcut durum: {Status}");

            // Süre kontrolü
            if (DateTime.UtcNow > ExpiresAt)
            {
                Expire();
                throw new InvalidOperationException("Match süresi dolduğu için kabul edilemedi.");
            }

            Status = MatchStatus.Accepted;
            RespondedAt = DateTime.UtcNow;

            // EVENT GENERATION: MatchAccepted
            AddDomainEvent(new MatchAcceptedEvent(Id, UserAId, UserBId));
        }

        // 2. REDDETME
        public void Reject()
        {
            if (Status != MatchStatus.Pending)
                throw new InvalidOperationException($"Match reddedilemez. Mevcut durum: {Status}");

            Status = MatchStatus.Rejected;
            RespondedAt = DateTime.UtcNow;

            // EVENT GENERATION: MatchRejected
            AddDomainEvent(new MatchRejectedEvent(Id, 0));
        }

        // 3. İPTAL ETME / EŞLEŞMEYİ BOZMA (Unmatch)
        public void Cancel()
        {
            if (Status != MatchStatus.Pending && Status != MatchStatus.Accepted)
                throw new InvalidOperationException($"Match iptal edilemez. Mevcut durum: {Status}");

            Status = MatchStatus.Cancelled;
            RespondedAt = DateTime.UtcNow;

            // EVENT GENERATION: MatchCancelled
            AddDomainEvent(new MatchCancelledEvent(Id));
        }

        // 4. SÜRE AŞIMI
        public void Expire()
        {
            if (Status != MatchStatus.Pending) return;

            // Guard: Süre gerçekten doldu mu?
            if (DateTime.UtcNow <= ExpiresAt)
                throw new InvalidOperationException("Henüz süresi dolmamış bir Match expire edilemez.");

            Status = MatchStatus.Expired;
            RespondedAt = DateTime.UtcNow;

            // EVENT GENERATION: MatchExpired
            AddDomainEvent(new MatchExpiredEvent(Id));
        }
    }
}