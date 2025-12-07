using SubscriptionSystem.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IPredictionRepository
    {
        Task<string> CreatePredictionAsync(Prediction prediction);
        Task<(IEnumerable<Prediction> predictions, int totalCount)> GetPredictionsAsync(int page, int pageSize);
        Task<(IEnumerable<Prediction> predictions, int totalCount)> GetPastPredictionsAsync(int page, int pageSize);
        Task<Prediction> GetPredictionByIdAsync(string id);
        Task UpdatePredictionAsync(Prediction prediction);
        Task<PredictionStatistics> GetPredictionStatisticsAsync();
        Task DeletePredictionAsync(string id);
    }

    public class PredictionStatistics
    {
        public int TotalPredictions { get; set; }
        public int WinningPredictions { get; set; }
        public int LosingPredictions { get; set; }
    }
}

