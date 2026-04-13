using System;

namespace Lunera.Domain.Events
{
    public record MatchCreatedEvent(int UserAId, int UserBId, DateTime ExpiresAt) : IInternalDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    public record MatchAcceptedEvent(int MatchId, int UserAId, int UserBId) : IInternalDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    public record MatchRejectedEvent(int MatchId, int UserAId, int UserBId) : IInternalDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    public record MatchCancelledEvent(int MatchId, int UserAId, int UserBId) : IInternalDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    public record MatchExpiredEvent(int MatchId, int UserAId, int UserBId) : IInternalDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}