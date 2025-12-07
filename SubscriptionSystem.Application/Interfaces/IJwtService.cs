using System.Security.Claims;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        bool ValidateToken(string token);
    }
}

