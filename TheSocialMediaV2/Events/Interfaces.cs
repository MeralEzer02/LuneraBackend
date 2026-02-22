namespace TheSocialMediaV2.API.Events
{
    public interface IInternalDomainEvent
    {
        Guid EventId { get; }
        DateTime OccurredOn { get; }
    }

    public interface IInternalDomainEventHandler<T> where T : IInternalDomainEvent
    {
        Task Handle(T domainEvent);
    }

    public interface IInternalDomainEventDispatcher
    {
        Task Dispatch<T>(T domainEvent) where T : IInternalDomainEvent;
    }
}