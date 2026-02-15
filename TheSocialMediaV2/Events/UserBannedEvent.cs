namespace TheSocialMediaV2.API.Events
{
    public class UserBannedEvent : IInternalDomainEvent
    {
        public int UserId { get; }
        public int DurationDays { get; } // 0 ise Perma
        public string Reason { get; }
        public DateTime OccurredOn { get; }

        public UserBannedEvent(int userId, int? durationDays, string reason)
        {
            UserId = userId;
            DurationDays = durationDays ?? 0;
            Reason = reason;
            OccurredOn = DateTime.UtcNow;
        }
    }
}