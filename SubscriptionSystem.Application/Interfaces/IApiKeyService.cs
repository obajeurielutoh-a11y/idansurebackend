using System.Threading.Tasks;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IApiKeyService
    {
        Task<User> GetUserByApiKeyAsync(string apiKey);
        Task<bool> ValidateApiKeyAsync(string apiKey);
        Task<string> GenerateApiKeyForUserAsync(string userId);
        Task<bool> RevokeApiKeyAsync(string apiKey);

    }
}

