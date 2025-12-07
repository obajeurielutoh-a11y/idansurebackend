using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SubscriptionSystem.Infrastructure.Repositories
{
    public class PredictionRepository : IPredictionRepository
    {
        private readonly ApplicationDbContext _context;

        public PredictionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Prediction> GetPredictionByIdAsync(string id)
        {
            if (Guid.TryParse(id, out Guid guidId))
            {
                return await _context.Predictions.FindAsync(guidId);
            }
            return null;
        }
        
        public async Task<string> CreatePredictionAsync(Prediction prediction)
        {
            // Ensure prediction.Id is initialized if necessary
            if (prediction.Id == Guid.Empty)
            {
                prediction.Id = Guid.NewGuid();
            }

            await _context.Predictions.AddAsync(prediction);
            await _context.SaveChangesAsync();

            // Return the ID as a string
            return prediction.Id.ToString();
        }

        public async Task<(IEnumerable<Prediction> predictions, int totalCount)> GetPredictionsAsync(int page, int pageSize)
        {
            var totalCount = await _context.Predictions.CountAsync();
            var predictions = await _context.Predictions
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (predictions, totalCount);
        }


        public async Task<(IEnumerable<Prediction> predictions, int totalCount)> GetPastPredictionsAsync(int page, int pageSize)
        {
            var threeDaysAgo = DateTime.UtcNow.AddDays(-3);

            var query = _context.Predictions
                .Where(p => p.Outcome.HasValue && p.CreatedAt <= threeDaysAgo); // Exclude recent predictions

            var totalCount = await query.CountAsync();

            var predictions = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (predictions, totalCount);
        }



        public async Task UpdatePredictionAsync(Prediction prediction)
        {
            _context.Predictions.Update(prediction);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePredictionAsync(string id)
        {
            if (Guid.TryParse(id, out Guid guidId))
            {
                var prediction = await _context.Predictions.FindAsync(guidId);
                if (prediction != null)
                {
                    _context.Predictions.Remove(prediction);
                    await _context.SaveChangesAsync();
                }
            }
        }


        public async Task<PredictionStatistics> GetPredictionStatisticsAsync()
        {
            var totalPredictions = await _context.Predictions.CountAsync();
            var winningPredictions = await _context.Predictions.CountAsync(p => p.Outcome == MatchOutcome.Win);
            var losingPredictions = await _context.Predictions.CountAsync(p => p.Outcome == MatchOutcome.Loss);

            return new PredictionStatistics
            {
                TotalPredictions = totalPredictions,
                WinningPredictions = winningPredictions,
                LosingPredictions = losingPredictions
            };
        }
    }
}

