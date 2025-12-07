
using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Infrastructure.Data;

namespace SubscriptionSystem.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        public async Task<User?> GetUserByGoogleIdAsync(string googleId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }
        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.Include(u => u.Subscriptions)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<User> GetUserByCustomerRefAsync(string customerRef)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == customerRef);
        }
      
        public async Task<User> GetUserByPhoneNumberAsync(string PhoneNumber)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == PhoneNumber);
        }

        public async Task<(IEnumerable<User> users, int totalCount)> GetUsersAsync(int page, int pageSize)
        {
            var totalCount = await _context.Users.CountAsync();
            var users = await _context.Users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }
     

        public async Task<User> GetUserByIdAsync(string userId)
        {
            return await _context.Users
                .Include(u => u.Subscriptions)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }


        public async Task<User> GetUserByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        }

        public async Task CreateUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasActiveSubscriptionAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Subscriptions)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Subscriptions == null)
            {
                return false;
            }

            return user.Subscriptions.Any(s => s.IsActive && s.ExpiryDate > DateTime.UtcNow);
        }

        public async Task<UserSubscriptionInfo> GetUserSubscriptionInfoAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Subscriptions)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Subscriptions == null || !user.Subscriptions.Any())
            {
                return new UserSubscriptionInfo
                {
                    IsActive = false,
                    ExpiryDate = null,
                    TotalAmountPaid = 0
                };
            }

            var activeSubscription = user.Subscriptions
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.ExpiryDate)
                .FirstOrDefault();

            return new UserSubscriptionInfo
            {
                IsActive = activeSubscription != null && activeSubscription.ExpiryDate > DateTime.UtcNow,
                ExpiryDate = activeSubscription?.ExpiryDate,
                TotalAmountPaid = user.Subscriptions.Sum(s => s.AmountPaid)
            };
        }

        public async Task DeleteUserAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<UserStatistics> GetUserStatisticsAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var subscribedUsers = await _context.Users
                .CountAsync(u => u.Subscriptions.Any(s => s.IsActive && s.ExpiryDate > DateTime.UtcNow));

            var payments = await _context.Payments.ToListAsync();
            var totalAmountPaid = payments.Sum(p => p.Amount);

            Console.WriteLine($"Total payments: {payments.Count}");
            Console.WriteLine($"Total amount paid: {totalAmountPaid}");

            foreach (var payment in payments)
            {
                Console.WriteLine($"Payment amount: {payment.Amount}");
            }

            return new UserStatistics
            {
                TotalUsers = totalUsers,
                SubscribedUsers = subscribedUsers,
                TotalAmountPaid = totalAmountPaid
            };
        }




        public async Task MarkAccountDeletionRequestAsCompletedAsync(string requestId)
        {
            var request = await _context.AccountDeletionRequests.FindAsync(requestId);
            if (request != null)
            {
                request.IsActive = false;
                request.CompletedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<AccountDeletionRequest> GetAccountDeletionRequestAsync(string userId)
        {
            return await _context.AccountDeletionRequests
                .FirstOrDefaultAsync(r => r.UserId == userId && r.IsActive);
        }

        public async Task<AccountDeletionRequest> CreateAccountDeletionRequestAsync(string userId)
        {
            var newRequest = new AccountDeletionRequest
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                RequestDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.AccountDeletionRequests.Add(newRequest);
            await _context.SaveChangesAsync();

            return newRequest;
        }

        public async Task<List<AccountDeletionRequest>> GetAccountDeletionRequestAsync()
        {
            return await _context.AccountDeletionRequests.ToListAsync();
        }

        public async Task<List<AccountDeletionRequest>> GetAllActiveAccountDeletionRequestsAsync()
        {
            return await _context.AccountDeletionRequests
                .Where(r => r.IsActive)
                .ToListAsync();
        }
        public async Task<bool> AnyPaymentRecordsAsync()
        {
            return await _context.PaymentRecords.AnyAsync();
        }

        public async Task<List<PaymentRecord>> GetPaymentRecordsAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.PaymentRecords
                .Where(p => p.TransactionDate >= startDate && p.TransactionDate <= endDate)
                .ToListAsync();
        }
        public async Task<UserStatistics> GetUserStatisticsAsync(DateTime startDate, string userType)
        {
            // Check if any users exist
            bool anyUsers = await _context.Users.AnyAsync();
            if (!anyUsers)
            {
                return new UserStatistics
                {
                    Message = "No records found",
                    TotalUsers = 0,
                    SubscribedUsers = 0,
                    TotalAmountPaid = 0,
                    UserGrowthData = new object[0],
                    RevenueData = new object[0],
                    SubscriberDistribution = new object[0]
                };
            }

            // Get total users without any date filter
            int totalUsers = await _context.Users.CountAsync();

            // Get subscribed users without any date filter
            int subscribedUsers = await _context.Users
                .Where(u => u.HasActiveSubscription)
                .CountAsync();

            // Check if any payments exist
            bool anyPayments = await _context.Payments.AnyAsync(p => p.Status == "completed");
            decimal totalAmountPaid = 0;

            if (anyPayments)
            {
                // Calculate total amount paid without date filter
                totalAmountPaid = await _context.Payments
                    .Where(p => p.Status == "completed")
                    .SumAsync(p => p.Amount);
            }

            // Generate user growth data (monthly breakdown)
            var userGrowthData = await GenerateUserGrowthDataAsync(startDate);

            // Generate revenue data (monthly breakdown)
            var revenueData = await GenerateRevenueDataAsync(startDate);

            // Generate subscriber distribution with actual numbers
            var subscriberDistribution = new[]
            {
        new { Name = "Regular Users", Value = totalUsers - subscribedUsers },
        new { Name = "Subscribed Users", Value = subscribedUsers }
    };

            return new UserStatistics
            {
                TotalUsers = totalUsers,
                SubscribedUsers = subscribedUsers,
                TotalAmountPaid = totalAmountPaid,
                UserGrowthData = userGrowthData,
                RevenueData = revenueData,
                SubscriberDistribution = subscriberDistribution
            };
        }
        private async Task<object[]> GenerateUserGrowthDataAsync(DateTime startDate)
        {
            var result = new List<object>();

            // Get all months from the earliest user to now
            var firstUserDate = await _context.Users
                .MinAsync(u => u.CreatedAt);

            var months = Enumerable.Range(0, 7)
                .Select(i => startDate.AddMonths(i))
                .Select(date => new DateTime(date.Year, date.Month, 1))
                .ToList();

            foreach (var currentMonth in months)
            {
                var nextMonth = currentMonth.AddMonths(1);

                // Get cumulative total up to this month
                var totalUsers = await _context.Users
                    .Where(u => u.CreatedAt < nextMonth)
                    .CountAsync();

                // Get new users for this month
                var newUsers = await _context.Users
                    .Where(u => u.CreatedAt >= currentMonth && u.CreatedAt < nextMonth)
                    .CountAsync();

                result.Add(new
                {
                    Name = currentMonth.ToString("MMM"),
                    Users = totalUsers,   // This should show the cumulative total
                    NewUsers = newUsers   // This should show only new users for that month
                });
            }

            return result.ToArray();
        }

        private async Task<object[]> GenerateRevenueDataAsync(DateTime startDate)
        {
            var result = new List<object>();

            var months = Enumerable.Range(0, 7)
                .Select(i => startDate.AddMonths(i))
                .Select(date => new DateTime(date.Year, date.Month, 1))
                .ToList();

            foreach (var currentMonth in months)
            {
                var nextMonth = currentMonth.AddMonths(1);

                // Get payments for this month
                var monthlyPayments = await _context.Payments
                    .Where(p => p.PaymentDate >= currentMonth &&
                               p.PaymentDate < nextMonth &&
                               p.Status == "completed")
                    .ToListAsync();

                // Calculate revenues by subscription type
                decimal dailySubscriptions = monthlyPayments
                    .Where(p => p.Amount == 100m)
                    .Sum(p => p.Amount);

                decimal weeklySubscriptions = monthlyPayments
                    .Where(p => p.Amount == 650m)
                    .Sum(p => p.Amount);

                decimal monthlySubscriptions = monthlyPayments
                    .Where(p => p.Amount == 2100m)
                    .Sum(p => p.Amount);

                decimal totalRevenue = dailySubscriptions + weeklySubscriptions + monthlySubscriptions;

                result.Add(new
                {
                    Name = currentMonth.ToString("MMM"),
                    Revenue = totalRevenue,
                    DailySubscriptions = dailySubscriptions,    // 100 naira subscriptions
                    WeeklySubscriptions = weeklySubscriptions,  // 650 naira subscriptions
                    MonthlySubscriptions = monthlySubscriptions // 2100 naira subscriptions
                });
            }

            return result.ToArray();
        }

        public async Task<PaymentStatistics> GetPaymentStatisticsAsync(DateTime startDate, DateTime? endDate = null)
        {
            if (endDate == null)
            {
                endDate = DateTime.Now;
            }

            // Check if any payment records exist
            bool anyPayments = await _context.PaymentRecords.AnyAsync();
            if (!anyPayments)
            {
                return new PaymentStatistics
                {
                    TotalTransactions = 0,
                    SuccessfulTransactions = 0,
                    FailedTransactions = 0,
                    TotalAmount = 0,
                    SuccessfulAmount = 0,
                    FailedTransactionDetails = new List<FailedTransaction>(),
                    DailyTransactions = new List<PaymentSummary>()
                };
            }

            // Get all payment records within the date range
            var payments = await _context.PaymentRecords
                .Where(p => p.TransactionDate >= startDate && p.TransactionDate <= endDate)
                .ToListAsync();

            if (payments.Count == 0)
            {
                return new PaymentStatistics
                {
                    TotalTransactions = 0,
                    SuccessfulTransactions = 0,
                    FailedTransactions = 0,
                    TotalAmount = 0,
                    SuccessfulAmount = 0,
                    FailedTransactionDetails = new List<FailedTransaction>(),
                    DailyTransactions = new List<PaymentSummary>()
                };
            }

            // Calculate overall statistics
            int totalTransactions = payments.Count;
            int successfulTransactions = payments.Count(p => p.ResponseCode == "00");
            int failedTransactions = payments.Count(p => p.ResponseCode == "01");
            decimal totalAmount = payments.Sum(p => p.Amount);
            decimal successfulAmount = payments.Where(p => p.ResponseCode == "00").Sum(p => p.Amount);

            // Get details of failed transactions
            var failedTransactionDetails = payments
                .Where(p => p.ResponseCode == "01")
                .Select(p => new FailedTransaction
                {
                    CustomerRef = p.CustomerRef,
                    PhoneNumber = p.CustomerRef, // Assuming CustomerRef is the phone number
                    Amount = p.Amount,
                    TransactionDate = p.TransactionDate,
                    ResponseCode = p.ResponseCode
                })
                .ToList();

            // Group transactions by date for daily summary
            var dailyTransactions = payments
                .GroupBy(p => p.TransactionDate.Date)
                .Select(g => new PaymentSummary
                {
                    Date = g.Key,
                    TotalCount = g.Count(),
                    SuccessfulCount = g.Count(p => p.ResponseCode == "00"),
                    FailedCount = g.Count(p => p.ResponseCode == "01"),
                    TotalAmount = g.Sum(p => p.Amount),
                    SuccessfulAmount = g.Where(p => p.ResponseCode == "00").Sum(p => p.Amount)
                })
                .OrderBy(s => s.Date)
                .ToList();

            return new PaymentStatistics
            {
                TotalTransactions = totalTransactions,
                SuccessfulTransactions = successfulTransactions,
                FailedTransactions = failedTransactions,
                TotalAmount = totalAmount,
                SuccessfulAmount = successfulAmount,
                FailedTransactionDetails = failedTransactionDetails,
                DailyTransactions = dailyTransactions
            };
        }
    }
}

