using TheSocialMediaV2.API.Events;

namespace TheSocialMediaV2.API.Events
{
    public class MatchCreatedEvent : IInternalDomainEvent
    {
        public Guid EventId { get; }
        public int MatchId { get; }
        public int RequesterId { get; }
        public int TargetId { get; }
        public DateTime CreatedAt { get; }
        public DateTime OccurredOn { get; }

        public MatchCreatedEvent(int matchId, int requesterId, int targetId)
        {
            EventId = Guid.NewGuid(); 
            MatchId = matchId;
            RequesterId = requesterId;
            TargetId = targetId;
            CreatedAt = DateTime.UtcNow;
            OccurredOn = DateTime.UtcNow;
        }
    }

    public class MatchAcceptedEvent : IInternalDomainEvent
    {
        public Guid EventId { get; } 
        public int MatchId { get; }
        public int UserAId { get; }
        public int UserBId { get; }
        public DateTime AcceptedAt { get; }
        public DateTime OccurredOn { get; }

        public MatchAcceptedEvent(int matchId, int userAId, int userBId)
        {
            EventId = Guid.NewGuid();
            MatchId = matchId;
            UserAId = userAId;
            UserBId = userBId;
            AcceptedAt = DateTime.UtcNow;
            OccurredOn = DateTime.UtcNow;
        }
    }

    public class MatchRejectedEvent : IInternalDomainEvent
    {
        public Guid EventId { get; } 
        public int MatchId { get; }
        public int RejectedById { get; }
        public DateTime RejectedAt { get; }
        public DateTime OccurredOn { get; }

        public MatchRejectedEvent(int matchId, int rejectedById)
        {
            EventId = Guid.NewGuid();
            MatchId = matchId;
            RejectedById = rejectedById;
            RejectedAt = DateTime.UtcNow;
            OccurredOn = DateTime.UtcNow;
        }
    }

    public class MatchCancelledEvent : IInternalDomainEvent
    {
        public Guid EventId { get; } 
        public int MatchId { get; }
        public DateTime CancelledAt { get; }
        public DateTime OccurredOn { get; }

        public MatchCancelledEvent(int matchId)
        {
            EventId = Guid.NewGuid();
            MatchId = matchId;
            CancelledAt = DateTime.UtcNow;
            OccurredOn = DateTime.UtcNow;
        }
    }

    public class MatchExpiredEvent : IInternalDomainEvent
    {
        public Guid EventId { get; } 
        public int MatchId { get; }
        public DateTime ExpiredAt { get; }
        public DateTime OccurredOn { get; }

        public MatchExpiredEvent(int matchId)
        {
            EventId = Guid.NewGuid();
            MatchId = matchId;
            ExpiredAt = DateTime.UtcNow;
            OccurredOn = DateTime.UtcNow;
        }
    }
}