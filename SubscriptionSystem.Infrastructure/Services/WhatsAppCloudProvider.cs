using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;

namespace SubscriptionSystem.Infrastructure.Services
{
    public class WhatsAppCloudProvider : IWhatsAppProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<WhatsAppCloudProvider> _logger;

        public WhatsAppCloudProvider(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<WhatsAppCloudProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public async Task SendMessageAsync(string toPhoneE164, string message, CancellationToken cancellationToken = default)
        {
            var token = _config["WhatsApp:Token"];
            var phoneNumberId = _config["WhatsApp:PhoneNumberId"];
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phoneNumberId))
            {
                _logger.LogWarning("WhatsApp config missing; skipping send");
                return;
            }

            var url = $"https://graph.facebook.com/v18.0/{phoneNumberId}/messages";
            var client = _httpClientFactory.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                messaging_product = "whatsapp",
                to = toPhoneE164,
                type = "text",
                text = new { body = message }
            };
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var resp = await client.SendAsync(req, cancellationToken);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("WhatsApp send failed {Status}: {Body}", (int)resp.StatusCode, body);
            }
        }
    }
}
