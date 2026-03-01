using System.ComponentModel.DataAnnotations;
using TheSocialMediaV2.API.Events;

namespace TheSocialMediaV2.API.Entities
{
    public enum MatchStatus { Pending = 1, Accepted = 2, Rejected = 3, Cancelled = 4, Expired = 5 }

    public class Match : IHasDomainEvents
    {
        public int Id { get; private set; }
        public int UserAId { get; private set; }
        public int UserBId { get; private set; }
        public MatchStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? RespondedAt { get; private set; }

        [Timestamp]
        public byte[] RowVersion { get; private set; }

        // Navigation Properties
        public User UserA { get; private set; }
        public User UserB { get; private set; }

        public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();
        private readonly List<Message> _messages = new();

        private readonly List<IInternalDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IInternalDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        private Match() { }

        public static Match Create(int userAId, int userBId, int expirationHours, DateTime utcNow)
        {
            if (userAId == userBId)
                throw new InvalidOperationException("Bir kullanıcı kendisiyle eşleşemez.");

            var match = new Match
            {
                UserAId = Math.Min(userAId, userBId),
                UserBId = Math.Max(userAId, userBId),
                Status = MatchStatus.Pending,
                CreatedAt = utcNow,
                ExpiresAt = utcNow.AddHours(expirationHours)
            };

            match.AddDomainEvent(new MatchCreatedEvent(match.UserAId, match.UserBId, match.ExpiresAt));
            match.EnsureInvariants();
            return match;
        }

        public void Accept(DateTime utcNow)
        {
            if (Status == MatchStatus.Expired || utcNow >= ExpiresAt)
            {
                Expire(utcNow);
                throw new InvalidOperationException("Eşleşme süresi dolduğu için kabul edilemez.");
            }
            if (Status != MatchStatus.Pending)
                throw new InvalidOperationException("Bu eşleşme şu anda kabul edilemez.");

            Status = MatchStatus.Accepted;
            RespondedAt = utcNow;
            AddDomainEvent(new MatchAcceptedEvent(Id, UserAId, UserBId));
            EnsureInvariants();
        }

        public void Reject(DateTime utcNow)
        {
            if (Status != MatchStatus.Pending)
                throw new InvalidOperationException("Bu eşleşme reddedilemez.");

            Status = MatchStatus.Rejected;
            RespondedAt = utcNow;
            AddDomainEvent(new MatchRejectedEvent(Id, UserAId, UserBId));
            EnsureInvariants();
        }

        public void Cancel(DateTime utcNow)
        {
            if (Status == MatchStatus.Cancelled || Status == MatchStatus.Rejected || Status == MatchStatus.Expired)
                throw new InvalidOperationException("Bu eşleşme iptal edilemez.");

            Status = MatchStatus.Cancelled;
            RespondedAt = utcNow;
            AddDomainEvent(new MatchCancelledEvent(Id, UserAId, UserBId));
            EnsureInvariants();
        }

        public void Expire(DateTime utcNow)
        {
            if (Status != MatchStatus.Pending) return;

            Status = MatchStatus.Expired;
            RespondedAt = utcNow;
            AddDomainEvent(new MatchExpiredEvent(Id, UserAId, UserBId));
            EnsureInvariants();
        }

        public void EnsureInvariants()
        {
            if (UserAId >= UserBId)
                throw new InvalidOperationException("INVARIANT VIOLATION: UserAId her zaman UserBId'den küçük olmalıdır.");

            if (Status != MatchStatus.Pending && RespondedAt == null)
                throw new InvalidOperationException("INVARIANT VIOLATION: İşlem görmüş eşleşmede RespondedAt boş olamaz.");
        }

        private void AddDomainEvent(IInternalDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        void IHasDomainEvents.ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}