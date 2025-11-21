using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SubscriptionSystem.Infrastructure.Services
{
    public class OpenAiChatProvider : IAiChatProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAiChatProvider> _logger;

        private const int MaxChars = 500;

        public OpenAiChatProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<OpenAiChatProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetResponseAsync(string userId, string message, string? tone, string? scope, string? context)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("OpenAI API key not configured");
                throw new InvalidOperationException("OpenAI API key not configured. Set OPENAI_API_KEY or OpenAI:ApiKey.");
            }

            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            // Raw multi-line prompt kept simple ASCII to avoid exotic unicode causing source issues.
            // Constraints enforced downstream (500 chars). Persona: IdanSure GPT, first-person, concise.
            var systemPrompt = @"You are IdanSure GPT - a friendly first-person football betting insight assistant.
Persona & tone:
- Speak as 'I'. Concise, upbeat, practical. No fluff.
- Confident but realistic (~80% assurance). Never guarantee wins.
- Promote disciplined bankroll and resilience after losses.

Style & constraints:
- Keep every reply under 500 characters, skimmable.
- Optional short personal opener when it adds warmth.
- Optionally include a one-line reminder: active subscription unlocks full predictions & WhatsApp alerts.
- If asked for unavailable real-time data, state limitation and give general actionable guidance.

Domain focus:
- Football only: recent form, injuries, head-to-head, home/away splits, congestion, odds/value.
- Offer quick angles: safer picks, value bets, notable risks tailored to user message.";

            var userTone = string.IsNullOrWhiteSpace(tone) ? "neutral" : tone.Trim();
            var userScope = string.IsNullOrWhiteSpace(scope) ? "football" : scope.Trim();
            var extraContext = string.IsNullOrWhiteSpace(context) ? string.Empty : $"\nContext: {context}";
            var userContent = $"User tone: {userTone}. Scope: {userScope}.{extraContext}\nMessage: {message}";

            var payload = new
            {
                model,
                temperature = 0.7,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userContent }
                }
            };

            var client = _httpClientFactory.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var resp = await client.SendAsync(req);
            var respBody = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI error {Status}: {Body}", (int)resp.StatusCode, respBody);
                throw new InvalidOperationException($"AI service error: {(int)resp.StatusCode}");
            }

            using var doc = JsonDocument.Parse(respBody);
            var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
            if (text.Length > MaxChars)
            {
                text = text.Substring(0, MaxChars);
            }
            return text;
        }
    }
}
