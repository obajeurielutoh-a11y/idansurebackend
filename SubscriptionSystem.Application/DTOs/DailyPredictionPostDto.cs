using System;

namespace SubscriptionSystem.Application.DTOs
{
    /// <summary>
    /// DTO for posting daily predictions (max 500 characters total)
    /// </summary>
    public class DailyPredictionPostDto
    {
        /// <summary>
        /// First team playing (alphanumeric)
        /// </summary>
        public string Team1 { get; set; } = string.Empty;

        /// <summary>
        /// Second team playing (alphanumeric)
        /// </summary>
        public string Team2 { get; set; } = string.Empty;

        /// <summary>
        /// Prediction outcome (alphanumeric)
        /// </summary>
        public string PredictionOutcome { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response DTO for daily predictions
    /// </summary>
    public class DailyPredictionResponseDto
    {
        public int Id { get; set; }
        public string Team1 { get; set; } = string.Empty;
        public string Team2 { get; set; } = string.Empty;
        public string PredictionOutcome { get; set; } = string.Empty;
        public DateTime PredictionDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Service result for prediction operations
    /// </summary>
    public class PredictionPostResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? PredictionId { get; set; }
    }
}
