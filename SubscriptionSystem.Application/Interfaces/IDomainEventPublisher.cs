using SubscriptionSystem.Domain.Events;
using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IDomainEventPublisher
    {
        Task PublishAsync(DomainEvent evt, CancellationToken cancellationToken = default);
    }
}
