using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using DomainMatchOutcome = SubscriptionSystem.Domain.Entities.MatchOutcome;

namespace SubscriptionSystem.Application.Services
{
    public class PredictionAnalyticsService : IPredictionAnalyticsService
    {
        private readonly IPredictionRepository _predictionRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PredictionAnalyticsService> _logger;

        public PredictionAnalyticsService(IPredictionRepository predictionRepository, IConfiguration configuration, ILogger<PredictionAnalyticsService> logger)
        {
            _predictionRepository = predictionRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<MonthlyPredictionAnalyticsDto> GetMonthlyAnalyticsAsync(int year, int month)
        {
            // Fetch predictions for the month via repository paging (we'll request large page sizes to get everything)
            var daily = new Dictionary<DateTime, (int wins, int losses)>();

            int page = 1;
            const int pageSize = 1000;
            while (true)
            {
                var (predictions, pageTotal) = await _predictionRepository.GetPredictionsAsync(page, pageSize);
                var monthItems = predictions.Where(p => p.CreatedAt.Year == year && p.CreatedAt.Month == month);
                foreach (var p in monthItems)
                {
                    var d = p.CreatedAt.Date;
                    if (!daily.ContainsKey(d)) daily[d] = (0, 0);
                    if (p.Outcome == DomainMatchOutcome.Win) daily[d] = (daily[d].wins + 1, daily[d].losses);
                    else if (p.Outcome == DomainMatchOutcome.Loss) daily[d] = (daily[d].wins, daily[d].losses + 1);
                }

                // break when fewer than pageSize returned
                if (predictions == null || predictions.Count() < pageSize) break;
                page++;
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

            // Optionally ask OpenAI for a human summary (if key present)
            var openAiKey = _configuration["OpenAI__ApiKey"] ?? _configuration["OpenAI:ApiKey"];
            if (!string.IsNullOrWhiteSpace(openAiKey))
            {
                try
                {
                    dto.OpenAIAnalysis = "(analysis disabled in this environment)";
                    // Optional: integrate call to OpenAI for natural language analysis
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OpenAI analysis failed");
                }
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
