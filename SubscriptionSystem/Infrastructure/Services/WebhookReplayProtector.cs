using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SubscriptionSystem.Infrastructure.Services
{
    /// <summary>
    /// Protects against webhook replay by storing body hashes in a distributed cache (Redis) or in-memory fallback.
    /// </summary>
    public class WebhookReplayProtector
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<WebhookReplayProtector> _logger;
        private readonly TimeSpan _ttl;

        public WebhookReplayProtector(IDistributedCache cache, IConfiguration configuration, ILogger<WebhookReplayProtector> logger)
        {
            _cache = cache;
            _logger = logger;
            var ttlSeconds = configuration.GetValue<int?>("WhatsApp:ReplayProtectionTTLSeconds") ?? 300;
            _ttl = TimeSpan.FromSeconds(ttlSeconds);
        }

        public static string ComputeBodyHashHex(string body)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(body ?? string.Empty);
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public async Task<bool> IsReplayAsync(string bodyHashHex)
        {
            try
            {
                var key = GetCacheKey(bodyHashHex);
                var existing = await _cache.GetAsync(key);
                return existing != null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Replay check failed, allowing through (cache error)");
                return false; // fail open - don't block processing if cache unavailable
            }
        }

        public async Task MarkProcessedAsync(string bodyHashHex)
        {
            try
            {
                var key = GetCacheKey(bodyHashHex);
                var payload = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("o"));
                var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _ttl };
                await _cache.SetAsync(key, payload, options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to mark webhook body as processed (cache error)");
            }
        }

        private static string GetCacheKey(string hashHex) => $"wh:{hashHex}";
    }
}
