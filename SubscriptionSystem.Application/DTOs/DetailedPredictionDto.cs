using System;
using System.ComponentModel.DataAnnotations;

namespace SubscriptionSystem.Application.DTOs
{
    public class DetailedPredictionDto
    {
        [Required]
        public string Tournament { get; set; }

        [Required]
        public string Team1 { get; set; }

        [Required]
        public string Team2 { get; set; }

        [Required]
        public DateTime MatchDate { get; set; }

        [Required]
        public string MatchDetails { get; set; }

        public string NonAlphanumericDetails { get; set; }

        [Required]
        public TeamPerformanceDto Team1Performance { get; set; }

        [Required]
        public TeamPerformanceDto Team2Performance { get; set; }

        [Required]
        [Range(0, 100)]
        public int ConfidenceLevel { get; set; }

        [Required]
        public string PredictedOutcome { get; set; }
    }

    public class TeamPerformanceDto
    {
        [Required]
        public int RecentWins { get; set; }

        [Required]
        public int RecentLosses { get; set; }

        [Required]
        public double AverageGoalsScored { get; set; }

        [Required]
        public double AverageGoalsConceded { get; set; }

        public string KeyPlayersStatus { get; set; }
    }
}

