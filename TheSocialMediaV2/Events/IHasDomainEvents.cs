using System.Collections.Generic;

namespace TheSocialMediaV2.API.Events
{
    public interface IHasDomainEvents
    {
        IReadOnlyCollection<IInternalDomainEvent> DomainEvents { get; }
        void ClearDomainEvents();
    }
}