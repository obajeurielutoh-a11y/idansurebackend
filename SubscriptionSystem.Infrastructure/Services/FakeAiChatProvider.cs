using SubscriptionSystem.Application.Interfaces;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubscriptionSystem.Infrastructure.Services
{
    // Simple fake AI provider for development/testing
    public class FakeAiChatProvider : IAiChatProvider
    {
        private const int MaxChars = 500;

        public Task<string> GetResponseAsync(string userId, string message, string? tone, string? scope, string? context)
        {
            var t = string.IsNullOrWhiteSpace(tone) ? "neutral" : tone!.Trim();
            var s = string.IsNullOrWhiteSpace(scope) ? "football" : scope!.Trim();
            var ctx = string.IsNullOrWhiteSpace(context) ? "" : $" | ctx: {context!.Trim()}";
            var sanitized = Regex.Replace(message ?? string.Empty, @"\s+", " ").Trim();
            if (sanitized.Length > 220) sanitized = sanitized.Substring(0, 220) + "...";

            var reply = $"[{s}/{t}] Quick take: {sanitized}. Stay disciplined, stake wisely.{ctx}";
            if (reply.Length > MaxChars)
            {
                reply = reply.Substring(0, MaxChars);
            }
            return Task.FromResult(reply);
        }
    }
}
