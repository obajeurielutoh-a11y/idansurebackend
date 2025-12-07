using SubscriptionSystem.Application.DTOs;
using System;
using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IAsedeyhotPredictionService
    {
        Task<ServiceResult<AsedeyhotPredictionDto>> CreatePredictionAsync(AsedeyhotPredictionDto predictionDto);
        Task<ServiceResult<AsedeyhotPredictionDto>> GetPredictionByIdAsync(Guid id);
        Task<ServiceResult<PaginatedResult<AsedeyhotPredictionDto>>> GetPredictionsAsync(int page, int pageSize);
        Task<ServiceResult<AsedeyhotPredictionDto>> UpdatePredictionResultAsync(Guid id, bool isWin, string resultDetails);
        Task<ServiceResult<bool>> DeletePredictionAsync(Guid id);
        Task<ServiceResult<AsedeyhotPredictionStatisticsDto>> GetPredictionStatisticsAsync();
        Task<ServiceResult<PaginatedResult<AsedeyhotPredictionDto>>> GetPastAndExpiredPredictionsAsync(int page, int pageSize);
    }
}

