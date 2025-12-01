using SubscriptionSystem.Application.DTOs;
using System;
using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IPredictionPostService
    {
        /// <summary>
        /// Create a daily prediction for the specified date
        /// </summary>
        Task<PredictionPostResult> CreateDailyPredictionAsync(string team1, string team2, string predictionOutcome, DateTime predictionDate);

        /// <summary>
        /// Get prediction for a specific date
        /// </summary>
        Task<DailyPredictionResponseDto?> GetPredictionForDateAsync(DateTime date);

        /// <summary>
        /// Check if msisdn has already received today's prediction (one per day security)
        /// </summary>
        Task<bool> HasReceivedTodaysPredictionAsync(string msisdn, DateTime date);

        /// <summary>
        /// Record that msisdn received today's prediction
        /// </summary>
        Task RecordPredictionSentAsync(string msisdn, int predictionId, DateTime sentDate);
    }
}
