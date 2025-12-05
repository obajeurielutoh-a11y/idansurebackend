using System.Threading.Tasks;
using SubscriptionSystem.Application.DTOs;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IPredictionAnalyticsService
    {
        Task<MonthlyPredictionAnalyticsDto> GetMonthlyAnalyticsAsync(int year, int month);
    }
}
