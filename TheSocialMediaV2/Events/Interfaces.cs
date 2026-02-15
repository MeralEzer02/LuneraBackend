namespace TheSocialMediaV2.API.Events
{
    public interface IInternalDomainEvent
    {
        DateTime OccurredOn { get; }
    }

    public interface IDomainEventHandler<T> where T : IInternalDomainEvent
    {
        Task Handle(T domainEvent);
    }

    public interface IDomainEventDispatcher
    {
        Task Dispatch<T>(T domainEvent) where T : IInternalDomainEvent;
    }
}