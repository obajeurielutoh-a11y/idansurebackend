using SubscriptionSystem.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Services
{
    public class TokenService : ITokenService
    {
        public async Task<bool> ValidateRefreshTokenAsync(string email, string refreshToken)
        {
            // Implement your token validation logic here
            // For now, we'll return true as a placeholder
            await Task.Delay(100); // Simulating some async work
            return true;
        }
    }
}
