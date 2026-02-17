using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheSocialMediaV2.API.Entities
{
    public enum MatchStatus
    {
        Pending = 1,  
        Accepted = 2, 
        Rejected = 3, 
        Cancelled = 4,
        Expired = 5   
    }

    public class Match
    {
        protected Match() { }

        private Match(int initiatorId, int targetId, int durationHours)
        {
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

            return new Match(initiatorId, targetId, durationHours);
        }

        // --- STATE TRANSITION METHODS (DAVRANIŞLAR & GUARDS) ---

        // 1. KABUL ETME
        public void Accept()
        {
            if (Status != MatchStatus.Pending)
                throw new InvalidOperationException($"Match kabul edilemez. Mevcut durum: {Status}");

            // Süre kontrolü
            if (DateTime.UtcNow > ExpiresAt)
            {
                Expire(); // Domain bütünlüğü için expire durumuna çek
                throw new InvalidOperationException("Match süresi dolduğu için kabul edilemedi.");
            }

            Status = MatchStatus.Accepted;
            RespondedAt = DateTime.UtcNow;
        }

        // 2. REDDETME
        public void Reject()
        {
            if (Status != MatchStatus.Pending)
                throw new InvalidOperationException($"Match reddedilemez. Mevcut durum: {Status}");

            Status = MatchStatus.Rejected;
            RespondedAt = DateTime.UtcNow;
        }

        // 3. İPTAL ETME / EŞLEŞMEYİ BOZMA (Unmatch)
        public void Cancel()
        {
            // Sadece Pending (İstekten vazgeçme) veya Accepted (Eşleşmeyi bozma) iptal edilebilir.
            if (Status != MatchStatus.Pending && Status != MatchStatus.Accepted)
                throw new InvalidOperationException($"Match iptal edilemez. Mevcut durum: {Status}");

            Status = MatchStatus.Cancelled;
            RespondedAt = DateTime.UtcNow;
        }

        // 4. SÜRE AŞIMI (Sistem Job'ı tarafından çağrılır)
        public void Expire()
        {
            // Sadece Pending durumundakiler Expire olabilir.
            // Zaten kabul edilmiş veya reddedilmişse süre işlemez.
            if (Status != MatchStatus.Pending)
                return;

            // Guard: Süre gerçekten doldu mu?
            if (DateTime.UtcNow <= ExpiresAt)
                throw new InvalidOperationException("Henüz süresi dolmamış bir Match expire edilemez.");

            Status = MatchStatus.Expired;
            RespondedAt = DateTime.UtcNow;
        }
    }
}