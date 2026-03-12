using System.Collections.Generic;

namespace TheSocialMediaV2.Domain.Events
{
    public interface IHasDomainEvents
    {
        IReadOnlyCollection<IInternalDomainEvent> DomainEvents { get; }
        void ClearDomainEvents();
    }
}