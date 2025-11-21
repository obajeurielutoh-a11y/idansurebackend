using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Application.Services
{
    public class MessageService : IMessageService
    {
        public Task<Message> CreateMessageAsync(string userName, string content)
        {
            var message = new Message
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Content = content,
                Timestamp = DateTime.UtcNow
            };

            return Task.FromResult(message);
        }
    }
}
