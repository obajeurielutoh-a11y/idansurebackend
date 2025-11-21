using SubscriptionSystem.Application.DTOs;
using System.Threading.Tasks;
namespace SubscriptionSystem.Application.Interfaces
{
    public interface IPredictionService
    {
        Task<ServiceResult<string>> CreateDetailedPredictionAsync(DetailedPredictionDto predictionDto);
        Task<ServiceResult<string>> CreateSimplePredictionAsync(SimplePredictionDto predictionDto);
        Task<PaginatedResult<PredictionResponseDto>> GetPredictionsAsync(int page, int pageSize);
        Task<PaginatedResult<PastPredictionDto>> GetPastPredictionsAsync(int page, int pageSize);
        Task<ServiceResult<bool>> AddMatchResultAsync(MatchResultDto resultDto);
        Task<PredictionStatisticsDto> GetPredictionStatisticsAsync();
        Task<ServiceResult<bool>> UpdatePredictionAsync(string id, DetailedPredictionDto predictionDto);
        Task<ServiceResult<bool>> UpdatePredictionOutcomeAsync(string id, Domain.Entities.MatchOutcome outcome);
        Task<ServiceResult<bool>> DeletePredictionAsync(string id);
    }
}

