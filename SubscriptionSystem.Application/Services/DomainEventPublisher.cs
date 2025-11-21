using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Events;

namespace SubscriptionSystem.Application.Services
{
    public class DomainEventPublisher : IDomainEventPublisher
    {
        private readonly IEnumerable<IDomainEventHandler<DomainEvent>> _handlers; // generic catch-all
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DomainEventPublisher> _logger;

        public DomainEventPublisher(IEnumerable<IDomainEventHandler<DomainEvent>> handlers, IServiceProvider serviceProvider, ILogger<DomainEventPublisher> logger)
        {
            _handlers = handlers;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task PublishAsync(DomainEvent evt, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Publishing domain event {EventType}", evt.GetType().Name);
                // Resolve concrete handlers dynamically
                var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(evt.GetType());
                var resolvedHandlers = (IEnumerable<object>)_serviceProvider.GetService(typeof(IEnumerable<>).MakeGenericType(handlerType)) ?? Enumerable.Empty<object>();

                foreach (var handler in resolvedHandlers)
                {
                    var method = handlerType.GetMethod("HandleAsync");
                    if (method != null)
                    {
                        var task = (Task)method.Invoke(handler, new object[] { evt, cancellationToken })!;
                        await task.ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing domain event {EventType}", evt.GetType().Name);
            }
        }
    }
}
