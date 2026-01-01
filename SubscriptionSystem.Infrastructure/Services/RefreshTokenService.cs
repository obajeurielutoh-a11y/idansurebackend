using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Infrastructure.Data;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Infrastructure.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(ApplicationDbContext db, ILogger<RefreshTokenService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<string> CreateRefreshTokenAsync(string userId, string createdByIp, TimeSpan? ttl = null)
        {
            var tokenValue = GenerateRandomToken(64);
            var rt = new RefreshToken
            {
                UserId = userId,
                Token = Hash(tokenValue),
                Expires = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromDays(30)),
                IsRevoked = false,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };
            _db.RefreshTokens.Add(rt);
            await _db.SaveChangesAsync();
            return tokenValue;
        }

        public async Task<(bool Success, string UserId)> ValidateRefreshTokenAsync(string token)
        {
            var hash = Hash(token);
            var rt = await _db.RefreshTokens
                .AsNoTracking()
                .Where(r => r.Token == hash)
                .FirstOrDefaultAsync();

            if (rt == null || rt.IsRevoked || rt.IsUsed || rt.Expires <= DateTime.UtcNow)
            {
                return (false, null);
            }

            return (true, rt.UserId);
        }

        public async Task RevokeRefreshTokenAsync(string token, string revokedByIp)
        {
            var hash = Hash(token);
            var rt = await _db.RefreshTokens.Where(r => r.Token == hash).FirstOrDefaultAsync();
            if (rt == null) return;
            rt.IsRevoked = true;
            rt.IsUsed = true;
            await _db.SaveChangesAsync();
        }

        public async Task RevokeAllForUserAsync(string userId, string revokedByIp)
        {
            var tokens = await _db.RefreshTokens.Where(r => r.UserId == userId && !r.IsRevoked).ToListAsync();
            foreach (var t in tokens)
            {
                t.IsRevoked = true;
                t.IsUsed = true;
            }
            await _db.SaveChangesAsync();
        }

        private static string GenerateRandomToken(int bytes = 32)
        {
            var data = new byte[bytes];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(data);
            return Convert.ToBase64String(data);
        }

        private static string Hash(string token)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }
    }
}
