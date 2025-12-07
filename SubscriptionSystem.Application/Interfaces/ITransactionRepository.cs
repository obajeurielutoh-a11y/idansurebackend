// File: SubscriptionSystem.Application/Interfaces/ITransactionRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Domain.Entities.SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface ITransactionRepository
    {
        Task<StandardizedTransaction> GetByIdAsync(string id);
        Task<StandardizedTransaction> GetByExternalIdAsync(string externalId, string gateway);
        Task<List<StandardizedTransaction>> GetByUserIdAsync(string userId);
        Task<List<StandardizedTransaction>> GetByEmailAsync(string email);
        Task<StandardizedTransaction> AddAsync(StandardizedTransaction transaction);
        Task<StandardizedTransaction> UpdateAsync(StandardizedTransaction transaction);
        Task<bool> DeleteAsync(string id);

        // Query methods
        IQueryable<StandardizedTransaction> GetAll();
        Task<int> CountAsync(Expression<Func<StandardizedTransaction, bool>> predicate = null);
        Task<List<StandardizedTransaction>> GetPagedAsync(
            int page,
            int pageSize,
            Expression<Func<StandardizedTransaction, bool>> predicate = null,
            Func<IQueryable<StandardizedTransaction>, IOrderedQueryable<StandardizedTransaction>> orderBy = null);

        // Aggregation methods
        Task<decimal> SumAmountAsync(Expression<Func<StandardizedTransaction, bool>> predicate = null);
        Task<List<GatewayStatDto>> GetGatewayStatsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null);
        Task<List<PlanTypeStatDto>> GetPlanTypeStatsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null);
        Task<List<DailyRevenueDto>> GetDailyRevenueAsync(
            DateTime startDate,
            DateTime endDate);
        Task<List<WeeklyRevenueDto>> GetWeeklyRevenueAsync(
            DateTime startDate,
            DateTime endDate);
        Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync(
            DateTime startDate,
            DateTime endDate, string status);
    }
}