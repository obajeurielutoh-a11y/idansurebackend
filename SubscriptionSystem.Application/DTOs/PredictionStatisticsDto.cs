namespace SubscriptionSystem.Application.DTOs
{
    public class PredictionStatisticsDto
    {
        public int TotalPredictions { get; set; }
        public int WinningPredictions { get; set; }
        public int LosingPredictions { get; set; }
        public double WinPercentage { get; set; }
    }
}

