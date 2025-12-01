using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SubscriptionSystem.Infrastructure.Services
{
    public class PredictionPostService : IPredictionPostService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PredictionPostService> _logger;

        public PredictionPostService(ApplicationDbContext context, ILogger<PredictionPostService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PredictionPostResult> CreateDailyPredictionAsync(string team1, string team2, string predictionOutcome, DateTime predictionDate)
        {
            try
            {
                // Normalize to date only (no time component)
                var dateOnly = predictionDate.Date;
                var today = DateTime.UtcNow.Date;

                // Only allow posting for today
                if (dateOnly != today)
                {
                    return new PredictionPostResult
                    {
                        Success = false,
                        Message = "You can only post predictions for today's date."
                    };
                }

                // Check if prediction already exists for today
                var existingPrediction = await _context.DailyPredictions
                    .FirstOrDefaultAsync(p => p.PredictionDate == dateOnly);

                if (existingPrediction != null)
                {
                    // Update existing prediction
                    existingPrediction.Team1 = team1;
                    existingPrediction.Team2 = team2;
                    existingPrediction.PredictionOutcome = predictionOutcome;
                    existingPrediction.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated daily prediction for {Date}. Prediction ID: {PredictionId}", dateOnly, existingPrediction.Id);

                    return new PredictionPostResult
                    {
                        Success = true,
                        Message = "Today's prediction updated successfully.",
                        PredictionId = existingPrediction.Id
                    };
                }

                // Create new prediction
                var newPrediction = new DailyPrediction
                {
                    Team1 = team1,
                    Team2 = team2,
                    PredictionOutcome = predictionOutcome,
                    PredictionDate = dateOnly,
                    CreatedAt = DateTime.UtcNow
                };

                _context.DailyPredictions.Add(newPrediction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new daily prediction for {Date}. Prediction ID: {PredictionId}", dateOnly, newPrediction.Id);

                return new PredictionPostResult
                {
                    Success = true,
                    Message = "Daily prediction created successfully.",
                    PredictionId = newPrediction.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating daily prediction");
                return new PredictionPostResult
                {
                    Success = false,
                    Message = $"An error occurred: {ex.Message}"
                };
            }
        }

        public async Task<DailyPredictionResponseDto?> GetPredictionForDateAsync(DateTime date)
        {
            try
            {
                var dateOnly = date.Date;
                var prediction = await _context.DailyPredictions
                    .FirstOrDefaultAsync(p => p.PredictionDate == dateOnly);

                if (prediction == null)
                    return null;

                return new DailyPredictionResponseDto
                {
                    Id = prediction.Id,
                    Team1 = prediction.Team1,
                    Team2 = prediction.Team2,
                    PredictionOutcome = prediction.PredictionOutcome,
                    PredictionDate = prediction.PredictionDate,
                    CreatedAt = prediction.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prediction for date {Date}", date);
                return null;
            }
        }

        public async Task<bool> HasReceivedTodaysPredictionAsync(string msisdn, DateTime date)
        {
            try
            {
                var dateOnly = date.Date;
                var hasReceived = await _context.PredictionDeliveryLogs
                    .AnyAsync(log => log.Msisdn == msisdn && log.SentDate == dateOnly);

                return hasReceived;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking prediction delivery status for {Msisdn}", msisdn);
                return false;
            }
        }

        public async Task RecordPredictionSentAsync(string msisdn, int predictionId, DateTime sentDate)
        {
            try
            {
                var dateOnly = sentDate.Date;

                // Check if already recorded (prevent duplicates)
                var exists = await _context.PredictionDeliveryLogs
                    .AnyAsync(log => log.Msisdn == msisdn && log.SentDate == dateOnly);

                if (exists)
                {
                    _logger.LogWarning("Prediction already recorded as sent to {Msisdn} on {Date}", msisdn, dateOnly);
                    return;
                }

                var log = new PredictionDeliveryLog
                {
                    Msisdn = msisdn,
                    PredictionId = predictionId,
                    SentDate = dateOnly,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PredictionDeliveryLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Recorded prediction {PredictionId} sent to {Msisdn} on {Date}", predictionId, msisdn, dateOnly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording prediction delivery for {Msisdn}", msisdn);
            }
        }
    }
}
