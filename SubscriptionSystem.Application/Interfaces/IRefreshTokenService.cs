using System;
using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<string> CreateRefreshTokenAsync(string userId, string createdByIp, TimeSpan? ttl = null);
        Task<(bool Success, string UserId)> ValidateRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token, string revokedByIp);
        Task RevokeAllForUserAsync(string userId, string revokedByIp);
    }
}
