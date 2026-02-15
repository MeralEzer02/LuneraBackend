using Microsoft.Extensions.DependencyInjection;

namespace TheSocialMediaV2.API.Events
{
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public DomainEventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Dispatch<T>(T domainEvent) where T : IInternalDomainEvent
        {
            var handlers = _serviceProvider.GetServices<IDomainEventHandler<T>>();

            foreach (var handler in handlers)
            {
                await handler.Handle(domainEvent);
            }
        }
    }
}