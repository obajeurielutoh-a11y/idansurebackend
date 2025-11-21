using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;

namespace SubscriptionSystem.Infrastructure.Repositories
{
    public class AsedeyhotPredictionRepository : IAsedeyhotPredictionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AsedeyhotPredictionRepository> _logger;
        private readonly IMemoryCache _cache;

        public AsedeyhotPredictionRepository(ApplicationDbContext context, ILogger<AsedeyhotPredictionRepository> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<AsedeyhotPrediction> CreatePredictionAsync(AsedeyhotPrediction prediction)
        {
            try
            {
                await _context.AsedeyhotPredictions.AddAsync(prediction);
                await _context.SaveChangesAsync();
                return prediction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while creating Asedeyhot prediction with ID: {prediction.Id}");
                throw;
            }
        }

        public async Task<AsedeyhotPrediction> GetPredictionByIdAsync(Guid id)
        {
            try
            {
                return await _context.AsedeyhotPredictions.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while retrieving Asedeyhot prediction with ID: {id}");
                throw;
            }
        }

        public async Task<IEnumerable<AsedeyhotPrediction>> GetPredictionsAsync(int page, int pageSize)
        {
            try
            {
                var cacheKey = $"PredictionsList_{page}_{pageSize}";
                if (_cache.TryGetValue(cacheKey, out IEnumerable<AsedeyhotPrediction> cachedPredictions))
                {
                    return cachedPredictions;
                }

                var predictions = await _context.AsedeyhotPredictions
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new AsedeyhotPrediction
                    {
                        Id = p.Id,
                        AlphanumericPrediction = p.AlphanumericPrediction ?? string.Empty,
                        NonAlphanumericDetails = p.NonAlphanumericDetails ?? string.Empty,
                        PredictedOutcome = p.PredictedOutcome ?? string.Empty,
                        IsPromotional = p.IsPromotional,
                        CreatedAt = p.CreatedAt,
                        IsWin = p.IsWin,
                        ResultDate = p.ResultDate,
                        ResultDetails = p.ResultDetails ?? string.Empty,
                        IsSuccess = p.IsSuccess,
                        Status = p.Status // Ensure this is included
                    })
                    .ToListAsync();

                // Cache the results
                _cache.Set(cacheKey, predictions, TimeSpan.FromMinutes(5));

                return predictions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving Asedeyhot predictions");
                throw;
            }
        }

        public async Task<AsedeyhotPrediction> UpdatePredictionAsync(AsedeyhotPrediction prediction)
        {
            try
            {
                _context.Entry(prediction).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return prediction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while updating Asedeyhot prediction with ID: {prediction.Id}");
                throw;
            }
        }

        public async Task DeletePredictionAsync(Guid id)
        {
            try
            {
                var prediction = await _context.AsedeyhotPredictions.FindAsync(id);
                if (prediction != null)
                {
                    _context.AsedeyhotPredictions.Remove(prediction);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting Asedeyhot prediction with ID: {id}");
                throw;
            }
        }

        public async Task<int> GetTotalCountAsync()
        {
            try
            {
                return await _context.AsedeyhotPredictions.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting total count of Asedeyhot predictions");
                throw;
            }
        }

        public async Task<IEnumerable<AsedeyhotPrediction>> GetPastAndExpiredPredictionsAsync(int page, int pageSize)
        {
            try
            {
                var currentDate = DateTime.UtcNow;
                return await _context.AsedeyhotPredictions
                    .AsNoTracking()
                    .Where(p => p.ResultDate.HasValue && p.ResultDate.Value < currentDate)
                    .OrderByDescending(p => p.ResultDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new AsedeyhotPrediction
                    {
                        Id = p.Id,
                        AlphanumericPrediction = p.AlphanumericPrediction ?? string.Empty,
                        NonAlphanumericDetails = p.NonAlphanumericDetails ?? string.Empty,
                        PredictedOutcome = p.PredictedOutcome ?? string.Empty,
                        IsPromotional = p.IsPromotional,
                        CreatedAt = p.CreatedAt,
                        IsWin = p.IsWin,
                        ResultDate = p.ResultDate,
                        ResultDetails = p.ResultDetails ?? string.Empty,
                        IsSuccess = p.IsSuccess,
                        Status = p.Status
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving past and expired Asedeyhot predictions");
                throw;
            }
        }
    }
}

