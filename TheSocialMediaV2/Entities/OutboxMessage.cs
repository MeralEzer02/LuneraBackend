using System.ComponentModel.DataAnnotations;

namespace TheSocialMediaV2.API.Entities
{
    public class OutboxMessage
    {
        public OutboxMessage(Guid id, DateTime occurredOnUtc, string type, string payload)
        {
            Id = id;
            OccurredOnUtc = occurredOnUtc;
            Type = type;
            Payload = payload;
            RetryCount = 0; // Default
            ProcessedOnUtc = null; // Default (Pending)
        }

        // EF Core Constructor
        protected OutboxMessage() { }

        public Guid Id { get; private set; }

        public DateTime OccurredOnUtc { get; private set; }

        [Required]
        public string Type { get; private set; } // Event Tipi (Full Name)

        [Required]
        public string Payload { get; private set; } // JSON Data

        public DateTime? ProcessedOnUtc { get; set; } // İşlendiği zaman (Null ise Pending)

        public string? Error { get; set; } // Hata detayı (Poison Message analizi için)

        public int RetryCount { get; set; } // Kaç kere denendi? (Backoff için)
    }
}