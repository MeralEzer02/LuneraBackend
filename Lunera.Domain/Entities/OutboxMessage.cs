using System;
using System.ComponentModel.DataAnnotations;

namespace Lunera.Domain.Entities
{
    public class OutboxMessage
    {
        public OutboxMessage(Guid id, Guid eventId, DateTime occurredOnUtc, string type, string payload)
        {
            Id = id;
            EventId = eventId;
            OccurredOnUtc = occurredOnUtc;
            Type = type;
            Payload = payload;
            RetryCount = 0; 
            ProcessedOnUtc = null;
        }

        // EF Core Constructor
        protected OutboxMessage() { }

        public Guid Id { get; private set; }

        public Guid EventId { get; private set; }

        public DateTime OccurredOnUtc { get; private set; }

        [Required]
        public string Type { get; private set; }

        [Required]
        public string Payload { get; private set; }

        public DateTime? ProcessedOnUtc { get; set; }

        public string? Error { get; set; }

        public int RetryCount { get; set; }
    }
}