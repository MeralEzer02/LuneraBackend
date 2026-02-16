namespace TheSocialMediaV2.API.Events
{
    public class UserBannedEvent : IInternalDomainEvent
    {
        public Guid EventId { get; } 
        public int UserId { get; }
        public int DurationDays { get; }
        public string Reason { get; }
        public DateTime OccurredOn { get; }

        public UserBannedEvent(int userId, int? durationDays, string reason)
        {
            EventId = Guid.NewGuid();
            UserId = userId;
            DurationDays = durationDays ?? 0;
            Reason = reason;
            OccurredOn = DateTime.UtcNow;
        }
    }
}