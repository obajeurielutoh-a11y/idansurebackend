using SubscriptionSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IAsedeyhotPredictionRepository
    {
        Task<AsedeyhotPrediction> CreatePredictionAsync(AsedeyhotPrediction prediction);
        Task<AsedeyhotPrediction> GetPredictionByIdAsync(Guid id);
        Task<IEnumerable<AsedeyhotPrediction>> GetPredictionsAsync(int page, int pageSize);
        Task<AsedeyhotPrediction> UpdatePredictionAsync(AsedeyhotPrediction prediction);
        Task DeletePredictionAsync(Guid id);
        Task<int> GetTotalCountAsync();
        Task<IEnumerable<AsedeyhotPrediction>> GetPastAndExpiredPredictionsAsync(int page, int pageSize);

    }
}

