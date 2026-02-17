using TheSocialMediaV2.API.Events;

namespace TheSocialMediaV2.API.Events
{
    public interface IDomainEvent { }

    public class MatchCreatedEvent : IDomainEvent
    {
        public int MatchId { get; }
        public int RequesterId { get; }
        public int TargetId { get; }
        public DateTime CreatedAt { get; }

        public MatchCreatedEvent(int matchId, int requesterId, int targetId)
        {
            MatchId = matchId;
            RequesterId = requesterId;
            TargetId = targetId;
            CreatedAt = DateTime.UtcNow;
        }
    }

    public class MatchAcceptedEvent : IDomainEvent
    {
        public int MatchId { get; }
        public int UserAId { get; }
        public int UserBId { get; }
        public DateTime AcceptedAt { get; }

        public MatchAcceptedEvent(int matchId, int userAId, int userBId)
        {
            MatchId = matchId;
            UserAId = userAId;
            UserBId = userBId;
            AcceptedAt = DateTime.UtcNow;
        }
    }

    public class MatchRejectedEvent : IDomainEvent
    {
        public int MatchId { get; }
        public int RejectedById { get; }
        public DateTime RejectedAt { get; }

        public MatchRejectedEvent(int matchId, int rejectedById)
        {
            MatchId = matchId;
            RejectedById = rejectedById;
            RejectedAt = DateTime.UtcNow;
        }
    }

    public class MatchCancelledEvent : IDomainEvent
    {
        public int MatchId { get; }
        public DateTime CancelledAt { get; }

        public MatchCancelledEvent(int matchId)
        {
            MatchId = matchId;
            CancelledAt = DateTime.UtcNow;
        }
    }

    public class MatchExpiredEvent : IDomainEvent
    {
        public int MatchId { get; }
        public DateTime ExpiredAt { get; }

        public MatchExpiredEvent(int matchId)
        {
            MatchId = matchId;
            ExpiredAt = DateTime.UtcNow;
        }
    }
}