using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.SignalR;
using SubscriptionSystem.Infrastructure.Hubs;


namespace SubscriptionSystem.Application.Services
{
    public class AsedeyhotPredictionService : IAsedeyhotPredictionService
    {
        private readonly IAsedeyhotPredictionRepository _repository;
        private readonly ILogger<AsedeyhotPredictionService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<PredictionHub> _hubContext;
        public AsedeyhotPredictionService(IAsedeyhotPredictionRepository repository, ILogger<AsedeyhotPredictionService> logger, IMemoryCache cache, IHubContext<PredictionHub> hubContext)
        {
            _repository = repository;
            _logger = logger;
            _cache = cache;
        
            _hubContext = hubContext;

        }
        public async Task<ServiceResult<AsedeyhotPredictionDto>> CreatePredictionAsync(AsedeyhotPredictionDto predictionDto)
        {
            try
            {
                var prediction = new AsedeyhotPrediction
                {
                    Id = Guid.NewGuid(),
                    AlphanumericPrediction = predictionDto.AlphanumericPrediction,
                    NonAlphanumericDetails = predictionDto.NonAlphanumericDetails,
                    PredictedOutcome = predictionDto.PredictedOutcome,
                    IsPromotional = predictionDto.IsPromotional,
                    CreatedAt = DateTime.UtcNow,
                    ResultDetails = predictionDto.ResultDetails,
                    ResultDate = predictionDto.IsWin.HasValue ? DateTime.UtcNow : null,
                    Status = predictionDto.IsWin.HasValue
                       ? (predictionDto.IsWin.Value ? PredictionStatus.Win : PredictionStatus.Loss)
                       : PredictionStatus.Pending,
                    IsWin = predictionDto.IsWin,
                    IsSuccess = predictionDto.IsWin.HasValue,
                };

                var createdPrediction = await _repository.CreatePredictionAsync(prediction);
                var createdPredictionDto = MapToDto(createdPrediction);
                int pageSize = 10;
                for (int i = 1; i <= 10; i++) // Clear first 10 pages as a reasonable default
                {
                    _cache.Remove($"PredictionsList_{i}_{pageSize}");
                }
                _cache.Remove("PredictionsList_All"); // Clear any "all predictions" cache

                return new ServiceResult<AsedeyhotPredictionDto>
                {
                    Data = createdPredictionDto,
                    IsSuccess = true,
                    Message = "Betslip created successfully and set to Pending."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating Betslip");
                return new ServiceResult<AsedeyhotPredictionDto>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while creating Betslip: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<AsedeyhotPredictionDto>> GetPredictionByIdAsync(Guid id)
        {
            try
            {
                // Try to get the prediction from cache first
                if (_cache.TryGetValue(id, out AsedeyhotPredictionDto cachedPrediction))
                {
                    return new ServiceResult<AsedeyhotPredictionDto>
                    {
                        Data = cachedPrediction,
                        IsSuccess = true,
                        Message = "Betslip retrieved from cache successfully."
                    };
                }

                // If not in cache, get from repository
                var prediction = await _repository.GetPredictionByIdAsync(id);
                if (prediction == null)
                {
                    return new ServiceResult<AsedeyhotPredictionDto>
                    {
                        IsSuccess = false,
                        Message = "Prediction not found."
                    };
                }

                var predictionDto = MapToDto(prediction);

                // Cache the prediction for future requests
                _cache.Set(id, predictionDto, TimeSpan.FromMinutes(5));

                return new ServiceResult<AsedeyhotPredictionDto>
                {
                    Data = predictionDto,
                    IsSuccess = true,
                    Message = "Betslip retrieved successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while retrieving Betslip with ID: {id}");
                return new ServiceResult<AsedeyhotPredictionDto>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while retrieving the Betslip: {ex.Message}"
                };
            }
        }


        public async Task<ServiceResult<PaginatedResult<AsedeyhotPredictionDto>>> GetPredictionsAsync(int page, int pageSize)
        {
            try
            {
                
                // If not in cache, get from repository
                var predictions = await _repository.GetPredictionsAsync(page, pageSize);
                var totalCount = predictions.Count();

                if (predictions == null)
                {
                    _logger.LogWarning("No Betslip found or an error occurred while retrieving Betslip.");
                    return new ServiceResult<PaginatedResult<AsedeyhotPredictionDto>>
                    {
                        IsSuccess = false,
                        Message = "No Betslip found or an error occurred while retrieving Betslip."
                    };
                }

                var predictionDtos = predictions.Select(MapToDto).ToList();

                var paginatedResult = new PaginatedResult<AsedeyhotPredictionDto>
                {
                    Items = predictionDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                // Cache the result
                _cache.Set(paginatedResult, TimeSpan.FromMinutes(5));

                return new ServiceResult<PaginatedResult<AsedeyhotPredictionDto>>
                {
                    Data = paginatedResult,
                    IsSuccess = true,
                    Message = "Betslips retrieved successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving Betslip");
                return new ServiceResult<PaginatedResult<AsedeyhotPredictionDto>>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while retrieving Betslip: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<AsedeyhotPredictionDto>> UpdatePredictionResultAsync(Guid id, bool isWin, string resultDetails)
        {
            try
            {
                var prediction = await _repository.GetPredictionByIdAsync(id);
                if (prediction == null)
                {
                    return new ServiceResult<AsedeyhotPredictionDto>
                    {
                        IsSuccess = false,
                        Message = "Betslip not found."
                    };
                }

                prediction.IsWin = isWin;
                prediction.ResultDate = DateTime.UtcNow;
                prediction.ResultDetails = resultDetails;
                prediction.IsSuccess = true;
                prediction.Status = isWin ? PredictionStatus.Win : PredictionStatus.Loss;

                var updatedPrediction = await _repository.UpdatePredictionAsync(prediction);
                var updatedPredictionDto = MapToDto(updatedPrediction);

                // Update the cache immediately
                _cache.Set(id, updatedPredictionDto, TimeSpan.FromMinutes(5));
                int pageSize = 10;
                for (int i = 1; i <= 10; i++) // Clear first 10 pages as a reasonable default
                {
                    _cache.Remove($"PredictionsList_{i}_{pageSize}");
                }
                _cache.Remove("PredictionsList_All"); // Clear any "all predictions" cache

                // Notify clients of the update
                await NotifyClientsOfUpdate(updatedPredictionDto);

                return new ServiceResult<AsedeyhotPredictionDto>
                {
                    Data = updatedPredictionDto,
                    IsSuccess = true,
                    Message = "Betslip updated successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while updating Betslip result with ID: {id}");
                return new ServiceResult<AsedeyhotPredictionDto>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while updating the Betslip result: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<bool>> DeletePredictionAsync(Guid id)
        {
            try
            {
                await _repository.DeletePredictionAsync(id);

                // Clear the specific prediction from cache
                _cache.Remove(id);

                // Clear paginated lists from cache
                int pageSize = 10;
                for (int i = 1; i <= 10; i++) // Clear first 10 pages as a reasonable default
                {
                    _cache.Remove($"PredictionsList_{i}_{pageSize}");
                }
                _cache.Remove("PredictionsList_All"); // Clear any "all predictions" cache

                // Notify clients of the deletion
                await _hubContext.Clients.All.SendAsync("PredictionDeleted", id);

                return new ServiceResult<bool>
                {
                    Data = true,
                    IsSuccess = true,
                    Message = "Betslip deleted successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting Betslip with ID: {id}");
                return new ServiceResult<bool>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while deleting the Betslip: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<AsedeyhotPredictionStatisticsDto>> GetPredictionStatisticsAsync()
        {
            try
            {
                var allPredictions = await _repository.GetPredictionsAsync(1, int.MaxValue);
                var completedPredictions = allPredictions.Where(p => p.IsSuccess).ToList();

                var statistics = new AsedeyhotPredictionStatisticsDto
                {
                    TotalPredictions = allPredictions.Count(),
                    CompletedPredictions = completedPredictions.Count,
                    WinningPredictions = completedPredictions.Count(p => p.IsWin == true),
                    LosingPredictions = completedPredictions.Count(p => p.IsWin == false)
                };

                return new ServiceResult<AsedeyhotPredictionStatisticsDto>
                {
                    Data = statistics,
                    IsSuccess = true,
                    Message = "Betslip statistics retrieved successfully."
                };
            }
            catch (Exception ex)
            {
          
                return new ServiceResult<AsedeyhotPredictionStatisticsDto>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while retrieving Betslip statistics: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<PaginatedResult<AsedeyhotPredictionDto>>> GetPastAndExpiredPredictionsAsync(int page, int pageSize)
        {
            try
            {
                // Calculate the timestamp for exactly 24 hours ago
                var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);
                var now = DateTime.UtcNow;

                // Get predictions from the repository
                var allPredictions = await _repository.GetPredictionsAsync(1, int.MaxValue);

                // Filter to only include predictions that:
                // 1. Were created more than 24 hours ago
                // 2. Have a result date in the past
                // 3. Are successful
                var filteredPredictions = allPredictions
                    .Where(p =>
                        p.CreatedAt < twentyFourHoursAgo &&
                        p.ResultDate < now &&
                        p.IsSuccess)
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var totalCount = allPredictions.Count(p =>
                    p.CreatedAt < twentyFourHoursAgo &&
                    p.ResultDate < now &&
                    p.IsSuccess);

                if (filteredPredictions == null || !filteredPredictions.Any())
                {
                    _logger.LogWarning("No past predictions found older than 24 hours.");
                    return new ServiceResult<PaginatedResult<AsedeyhotPredictionDto>>
                    {
                        IsSuccess = true, // Still successful, just empty
                        Message = "No past predictions found older than 24 hours.",
                        Data = new PaginatedResult<AsedeyhotPredictionDto>
                        {
                            Items = new List<AsedeyhotPredictionDto>(),
                            TotalCount = 0,
                            Page = page,
                            PageSize = pageSize
                        }
                    };
                }

                var predictionDtos = filteredPredictions.Select(MapToDto).ToList();

                var paginatedResult = new PaginatedResult<AsedeyhotPredictionDto>
                {
                    Items = predictionDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return new ServiceResult<PaginatedResult<AsedeyhotPredictionDto>>
                {
                    Data = paginatedResult,
                    IsSuccess = true,
                    Message = "Past predictions older than 24 hours retrieved successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving past predictions");
                return new ServiceResult<PaginatedResult<AsedeyhotPredictionDto>>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while retrieving past predictions: {ex.Message}"
                };
            }
        }
        private async Task NotifyClientsOfUpdate(AsedeyhotPredictionDto updatedPrediction)
        {
            await _hubContext.Clients.All.SendAsync("PredictionUpdated", updatedPrediction);
        }
        private AsedeyhotPredictionDto MapToDto(AsedeyhotPrediction prediction)
        {
            return new AsedeyhotPredictionDto
            {
                Id = prediction.Id,
                AlphanumericPrediction = prediction.AlphanumericPrediction ?? string.Empty,
                NonAlphanumericDetails = prediction.NonAlphanumericDetails ?? string.Empty,
                PredictedOutcome = prediction.PredictedOutcome ?? string.Empty,
                IsPromotional = prediction.IsPromotional,
                CreatedAt = prediction.CreatedAt,
                IsWin = prediction.IsWin,
                ResultDate = prediction.ResultDate,
                ResultDetails = prediction.ResultDetails ?? string.Empty,
                IsSuccess = prediction.IsSuccess,
                Status = prediction.Status
            };
        }
    }
}

