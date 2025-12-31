using System;
using System.Collections.Generic;

namespace SubscriptionSystem.Application.DTOs
{
    public class DailyPredictionMetric
    {
        public DateTime Date { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Total => Wins + Losses;
        public double SuccessRate => Total == 0 ? 0 : (double)Wins / Total * 100.0;
        public double MovingAverage { get; set; }
    }

    public class MonthlyPredictionAnalyticsDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public List<DailyPredictionMetric> DailyMetrics { get; set; } = new List<DailyPredictionMetric>();
        public double OverallSuccessRate { get; set; }
        public double TrendSlope { get; set; }
        public string? OpenAIAnalysis { get; set; }
        public string? Summary { get; set; }
        public string? Forecast { get; set; }
    }
}
