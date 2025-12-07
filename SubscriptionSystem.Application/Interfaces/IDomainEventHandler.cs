using System.Threading.Tasks;
using SubscriptionSystem.Domain.Events;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IDomainEventHandler<TEvent> where TEvent : DomainEvent
    {
        Task HandleAsync(TEvent evt, CancellationToken cancellationToken = default);
    }
}
