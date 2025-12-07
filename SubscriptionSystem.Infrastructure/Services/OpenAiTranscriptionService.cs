using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SubscriptionSystem.Infrastructure.Services
{
    public class OpenAiTranscriptionService : ITranscriptionService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAiTranscriptionService> _logger;

        public OpenAiTranscriptionService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<OpenAiTranscriptionService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> TranscribeAsync(Stream audioStream, string fileName, string? language = null, CancellationToken ct = default)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OpenAI API key not configured.");

            var client = _httpClientFactory.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(audioStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent("whisper-1"), "model");
            if (!string.IsNullOrWhiteSpace(language))
            {
                form.Add(new StringContent(language), "language");
            }
            req.Content = form;

            using var resp = await client.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI transcription failed {Status}: {Body}", (int)resp.StatusCode, body);
                throw new InvalidOperationException($"Transcription error: {(int)resp.StatusCode}");
            }

            // Response JSON: { "text": "..." }
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                var text = doc.RootElement.GetProperty("text").GetString();
                return text ?? string.Empty;
            }
            catch
            {
                return body; // fallback if API returns raw text
            }
        }
    }
}
