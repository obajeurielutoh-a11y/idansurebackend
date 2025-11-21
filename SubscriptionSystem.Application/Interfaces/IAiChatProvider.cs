using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IAiChatProvider
    {
        Task<string> GetResponseAsync(string userId, string message, string? tone, string? scope, string? context);
    }
}
