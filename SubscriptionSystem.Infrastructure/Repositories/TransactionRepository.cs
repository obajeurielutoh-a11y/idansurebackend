// File: SubscriptionSystem.Infrastructure/Repositories/TransactionRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Domain.Entities.SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Infrastructure.Data;

namespace SubscriptionSystem.Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public TransactionRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<StandardizedTransaction> GetByIdAsync(string id)
        {
            return await _context.Transactions.FindAsync(id);
        }

        public async Task<StandardizedTransaction> GetByExternalIdAsync(string externalId, string gateway)
        {
            return await _context.Transactions
                .FirstOrDefaultAsync(t => t.ExternalTransactionId == externalId && t.PaymentGateway == gateway);
        }

        public async Task<List<StandardizedTransaction>> GetByUserIdAsync(string userId)
        {
            return await _context.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<StandardizedTransaction>> GetByEmailAsync(string email)
        {
            return await _context.Transactions
                .Where(t => t.Email == email)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<StandardizedTransaction> AddAsync(StandardizedTransaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<StandardizedTransaction> UpdateAsync(StandardizedTransaction transaction)
        {
            _context.Entry(transaction).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
                return false;

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public IQueryable<StandardizedTransaction> GetAll()
        {
            return _context.Transactions.AsQueryable();
        }

        public async Task<int> CountAsync(Expression<Func<StandardizedTransaction, bool>> predicate = null)
        {
            return predicate == null
                ? await _context.Transactions.CountAsync()
                : await _context.Transactions.CountAsync(predicate);
        }

        public async Task<List<StandardizedTransaction>> GetPagedAsync(
            int page,
            int pageSize,
            Expression<Func<StandardizedTransaction, bool>> predicate = null,
            Func<IQueryable<StandardizedTransaction>, IOrderedQueryable<StandardizedTransaction>> orderBy = null)
        {
            IQueryable<StandardizedTransaction> query = _context.Transactions;

            if (predicate != null)
                query = query.Where(predicate);

            if (orderBy != null)
                query = orderBy(query);
            else
                query = query.OrderByDescending(t => t.CreatedAt);

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<decimal> SumAmountAsync(Expression<Func<StandardizedTransaction, bool>> predicate = null)
        {
            IQueryable<StandardizedTransaction> query = _context.Transactions;

            if (predicate != null)
                query = query.Where(predicate);

            return await query.SumAsync(t => t.Amount);
        }

        public async Task<List<GatewayStatDto>> GetGatewayStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            // Remove the Status filter to include all transactions
            IQueryable<StandardizedTransaction> query = _context.Transactions;

            // Use CreatedAt instead of CompletedAt since failed transactions won't have CompletedAt
            if (startDate.HasValue)
                query = query.Where(t => t.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.CreatedAt <= endDate.Value);

            var result = await query
                .GroupBy(t => t.PaymentGateway)
                .Select(g => new GatewayStatDto
                {
                    Gateway = g.Key,
                    Count = g.Count(),
                    Amount = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            return result;
        }
        public async Task<List<PlanTypeStatDto>> GetPlanTypeStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            // Remove the Status filter to include all transactions
            IQueryable<StandardizedTransaction> query = _context.Transactions;

            // Use CreatedAt instead of CompletedAt
            if (startDate.HasValue)
                query = query.Where(t => t.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.CreatedAt <= endDate.Value);

            var result = await query
                .GroupBy(t => t.PlanType)
                .Select(g => new PlanTypeStatDto
                {
                    PlanType = g.Key,
                    Count = g.Count(),
                    Amount = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            return result;
        }

        // Implement the revenue reporting methods as needed
        public async Task<List<DailyRevenueDto>> GetDailyRevenueAsync(DateTime startDate, DateTime endDate)
        {
            // Remove the Status filter to include all transactions
            var data = await _context.Transactions
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate) // Use CreatedAt instead of CompletedAt
                .GroupBy(t => new {
                    Date = t.CreatedAt.Date, // Use CreatedAt instead of CompletedAt
                    Gateway = t.PaymentGateway
                })
                .Select(g => new {
                    Date = g.Key.Date,
                    Gateway = g.Key.Gateway,
                    Amount = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            var result = data
                .GroupBy(r => r.Date)
                .Select(g => new DailyRevenueDto
                {
                    Date = g.Key,
                    Credo = g.Where(x => x.Gateway == "Credo").Sum(x => x.Amount),
                    AlatPay = g.Where(x => x.Gateway == "AlatPay").Sum(x => x.Amount),
                    CoralPay = g.Where(x => x.Gateway == "CoralPay").Sum(x => x.Amount),
                    Total = g.Sum(x => x.Amount)
                })
                .OrderBy(r => r.Date)
                .ToList();

            return result;
        }

        public async Task<List<WeeklyRevenueDto>> GetWeeklyRevenueAsync(DateTime startDate, DateTime endDate)
        {
            // Remove the Status filter to include all transactions
            var data = await _context.Transactions
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate) // Use CreatedAt instead of CompletedAt
                .ToListAsync();

            var result = data
                .GroupBy(t => {
                    var date = t.CreatedAt; // Use CreatedAt instead of CompletedAt.Value
                                            // Get the week start date (Sunday)
                    var diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
                    return date.AddDays(-1 * diff).Date;
                })
                .Select(g => new WeeklyRevenueDto
                {
                    Date = g.Key,
                    Credo = g.Where(t => t.PaymentGateway == "Credo").Sum(t => t.Amount),
                    AlatPay = g.Where(t => t.PaymentGateway == "AlatPay").Sum(t => t.Amount),
                    CoralPay = g.Where(t => t.PaymentGateway == "CoralPay").Sum(t => t.Amount),
                    Total = g.Sum(t => t.Amount)
                })
                .OrderBy(r => r.Date)
                .ToList();

            return result;
        }

        public async Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync(
     DateTime startDate,
     DateTime endDate,
     string status = null)
        {
            var query = _context.Transactions
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate);

            // Only apply status filter if provided
            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status == status);

            var data = await query.ToListAsync();

            var result = data
                .GroupBy(t => new DateTime(t.CreatedAt.Year, t.CreatedAt.Month, 1))
                .Select(g => new MonthlyRevenueDto
                {
                    Date = g.Key,
                    Credo = g.Where(t => t.PaymentGateway == "Credo").Sum(t => t.Amount),
                    AlatPay = g.Where(t => t.PaymentGateway == "AlatPay").Sum(t => t.Amount),
                    CoralPay = g.Where(t => t.PaymentGateway == "CoralPay").Sum(t => t.Amount),
                    Total = g.Sum(t => t.Amount)
                })
                .OrderBy(r => r.Date)
                .ToList();

            return result;
        }
    }
}