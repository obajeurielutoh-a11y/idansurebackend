using System;

namespace SubscriptionSystem.Application.DTOs
{
    public class AsedeyhotPredictionStatisticsDto
    {
        public int TotalPredictions { get; set; }
        public int WinningPredictions { get; set; }
        public int LosingPredictions { get; set; }
        public int PendingPredictions { get; set; }
        public decimal WinPercentage { get; set; }
        public int CompletedPredictions { get; set; }
    }
}

