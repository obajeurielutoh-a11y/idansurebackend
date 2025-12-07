using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IMessageService
    {
        Task<Message> CreateMessageAsync(string userName, string content);
    }
}
