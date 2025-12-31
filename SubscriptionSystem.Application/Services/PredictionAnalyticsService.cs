using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using DomainMatchOutcome = SubscriptionSystem.Domain.Entities.MatchOutcome;

namespace SubscriptionSystem.Application.Services
{
    public class PredictionAnalyticsService : IPredictionAnalyticsService
    {
        private readonly IPredictionRepository _predictionRepository;
        private readonly SubscriptionSystem.Application.Interfaces.IAsedeyhotPredictionRepository _asedeyhotRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PredictionAnalyticsService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public PredictionAnalyticsService(IPredictionRepository predictionRepository, SubscriptionSystem.Application.Interfaces.IAsedeyhotPredictionRepository asedeyhotRepository, IConfiguration configuration, ILogger<PredictionAnalyticsService> logger, IHttpClientFactory httpClientFactory)
        {
            _predictionRepository = predictionRepository;
            _asedeyhotRepository = asedeyhotRepository;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<MonthlyPredictionAnalyticsDto> GetMonthlyAnalyticsAsync(int year, int month)
        {
            // Fetch predictions for the month via repository paging (we'll request large page sizes to get everything)
            var daily = new Dictionary<DateTime, (int wins, int losses)>();

            // Aggregate from detailed predictions
            int page = 1;
            const int pageSize = 1000;
            while (true)
            {
                var (predictions, pageTotal) = await _predictionRepository.GetPredictionsAsync(page, pageSize);
                var monthItems = predictions?.Where(p => p.CreatedAt.Year == year && p.CreatedAt.Month == month) ?? Enumerable.Empty<SubscriptionSystem.Domain.Entities.Prediction>();
                foreach (var p in monthItems)
                {
                    var d = p.CreatedAt.Date;
                    if (!daily.ContainsKey(d)) daily[d] = (0, 0);
                    if (p.Outcome == DomainMatchOutcome.Win) daily[d] = (daily[d].wins + 1, daily[d].losses);
                    else if (p.Outcome == DomainMatchOutcome.Loss) daily[d] = (daily[d].wins, daily[d].losses + 1);
                }

                if (predictions == null || predictions.Count() < pageSize) break;
                page++;
            }

            // Aggregate from Asedeyhot predictions
            try
            {
                var asedeyhot = await _asedeyhotRepository.GetPredictionsAsync(1, int.MaxValue);
                foreach (var a in asedeyhot.Where(p => p.CreatedAt.Year == year && p.CreatedAt.Month == month))
                {
                    var d = a.CreatedAt.Date;
                    if (!daily.ContainsKey(d)) daily[d] = (0, 0);
                    if (a.IsWin == true) daily[d] = (daily[d].wins + 1, daily[d].losses);
                    else if (a.IsWin == false) daily[d] = (daily[d].wins, daily[d].losses + 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to aggregate Asedeyhot predictions for analytics");
            }

            // build DTO
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var dto = new MonthlyPredictionAnalyticsDto { Year = year, Month = month };
            var metrics = new List<DailyPredictionMetric>();
            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(year, month, d);
                daily.TryGetValue(date, out var vals);
                metrics.Add(new DailyPredictionMetric { Date = date, Wins = vals.wins, Losses = vals.losses, MovingAverage = 0 });
            }

            // compute moving average (7-day)
            for (int i = 0; i < metrics.Count; i++)
            {
                var window = metrics.Skip(Math.Max(0, i - 6)).Take(7).Select(x => x.SuccessRate);
                metrics[i].MovingAverage = window.Any() ? window.Average() : 0;
            }

            dto.DailyMetrics = metrics;

            // overall
            var totalWins = metrics.Sum(m => m.Wins);
            var totalLosses = metrics.Sum(m => m.Losses);
            var overallTotal = totalWins + totalLosses;
            dto.OverallSuccessRate = overallTotal == 0 ? 0 : (double)totalWins / overallTotal * 100.0;

            // simple trend slope (linear regression on success rate)
            var xs = metrics.Select((m, idx) => (double)idx).ToArray();
            var ys = metrics.Select(m => m.SuccessRate).ToArray();
            dto.TrendSlope = CalculateSlope(xs, ys);

            // Summary and simple forecast based on aggregates
            dto.Summary = totalWins + totalLosses == 0
                ? "No predictions recorded for the month." 
                : $"Total predictions: {overallTotal}, Wins: {totalWins}, Losses: {totalLosses}, SuccessRate: {Math.Round(dto.OverallSuccessRate,2)}%.";

            if (overallTotal == 0)
            {
                dto.Forecast = "Insufficient data to forecast performance.";
            }
            else if (dto.TrendSlope > 0.001 && dto.OverallSuccessRate >= 50)
            {
                dto.Forecast = "Performance is improving and likely to continue improving.";
            }
            else if (dto.TrendSlope > 0)
            {
                dto.Forecast = "Early signs of improvement; continue monitoring.";
            }
            else if (dto.TrendSlope < 0)
            {
                dto.Forecast = "Performance is declining; consider model tuning or data review.";
            }
            else
            {
                dto.Forecast = "Performance is flat; no clear trend detected.";
            }

            // Optionally ask OpenAI for a human summary (if key present)
            var openAiKey = _configuration["OpenAI__ApiKey"] ?? _configuration["OpenAI:ApiKey"];
            if (!string.IsNullOrWhiteSpace(openAiKey))
            {
                try
                {
                    // prepare compact metrics payload
                    var compact = metrics.Select(m => new { date = m.Date.ToString("yyyy-MM-dd"), wins = m.Wins, losses = m.Losses, successRate = Math.Round(m.SuccessRate, 3) }).ToArray();
                    var payloadJson = JsonSerializer.Serialize(new
                    {
                        year,
                        month,
                        overallSuccessRate = Math.Round(dto.OverallSuccessRate, 3),
                        trendSlope = Math.Round(dto.TrendSlope, 6),
                        daily = compact
                    });

                    var model = _configuration["OpenAI__Model"] ?? _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
                    var client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAiKey);

                    var systemPrompt = "You are a data analyst. Provide a concise, actionable summary of prediction performance for a month: highlight strengths, weaknesses, trend, notable days, and suggest model tuning directions. Output JSON with keys: summary, recommendations, highlights.";
                    var userPrompt = $"Here is the monthly metrics JSON:\n{payloadJson}\nProduce the requested JSON output.";

                    var reqBody = new
                    {
                        model = model,
                        messages = new[]
                        {
                            new { role = "system", content = systemPrompt },
                            new { role = "user", content = userPrompt }
                        },
                        temperature = 0.2,
                        max_tokens = 500
                    };

                    var reqJson = JsonSerializer.Serialize(reqBody);
                    using var content = new StringContent(reqJson, Encoding.UTF8, "application/json");
                    using var resp = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                    var respText = await resp.Content.ReadAsStringAsync();

                    if (resp.IsSuccessStatusCode)
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(respText);
                            var message = doc.RootElement
                                             .GetProperty("choices")[0]
                                             .GetProperty("message")
                                             .GetProperty("content")
                                             .GetString();
                            dto.OpenAIAnalysis = message ?? "(no content)";
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse OpenAI response");
                            dto.OpenAIAnalysis = "(openai returned unparsable response)";
                        }
                    }
                    else
                    {
                        _logger.LogWarning("OpenAI request failed: {Status} {Body}", resp.StatusCode, respText);
                        dto.OpenAIAnalysis = $"(openai error: {resp.StatusCode})";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OpenAI analysis failed");
                    dto.OpenAIAnalysis = "(openai analysis error)";
                }
            }
            else
            {
                dto.OpenAIAnalysis = "(analysis disabled in this environment)";
            }

            return dto;
        }

        private static double CalculateSlope(double[] x, double[] y)
        {
            if (x.Length < 2) return 0;
            var n = x.Length;
            var xAvg = x.Average();
            var yAvg = y.Average();
            var num = 0.0;
            var den = 0.0;
            for (int i = 0; i < n; i++)
            {
                num += (x[i] - xAvg) * (y[i] - yAvg);
                den += (x[i] - xAvg) * (x[i] - xAvg);
            }
            return den == 0 ? 0 : num / den;
        }
    }
}
