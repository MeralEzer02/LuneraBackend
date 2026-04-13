using System.Collections.Generic;

namespace Lunera.Domain.Events
{
    public interface IHasDomainEvents
    {
        IReadOnlyCollection<IInternalDomainEvent> DomainEvents { get; }
        void ClearDomainEvents();
    }
}