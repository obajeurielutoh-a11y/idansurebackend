using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserByPhoneNumberAsync(string CustomerRef);

        Task<(IEnumerable<User> users, int totalCount)> GetUsersAsync(int page, int pageSize);
        Task<User> GetUserByIdAsync(string userId);

        Task<User> GetUserByEmailAsync(string email);
       
        Task SaveChangesAsync();
        Task<User> GetUserByRefreshTokenAsync(string refreshToken);
        Task CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<bool> HasActiveSubscriptionAsync(string userId);
        Task<UserSubscriptionInfo> GetUserSubscriptionInfoAsync(string userId);
        Task DeleteUserAsync(string userId);
        Task<UserStatistics> GetUserStatisticsAsync(DateTime startDate, string userType);

        Task MarkAccountDeletionRequestAsCompletedAsync(string requestId);
        Task<AccountDeletionRequest> GetAccountDeletionRequestAsync(string userId);
        Task<AccountDeletionRequest> CreateAccountDeletionRequestAsync(string userId);
        Task<List<AccountDeletionRequest>> GetAccountDeletionRequestAsync();
        Task<List<AccountDeletionRequest>> GetAllActiveAccountDeletionRequestsAsync();
        Task<User> GetUserByCustomerRefAsync(string customerRef);
        Task<bool> AnyPaymentRecordsAsync();
        Task<List<PaymentRecord>> GetPaymentRecordsAsync(DateTime startDate, DateTime endDate);
        Task<User?> GetUserByGoogleIdAsync(string googleId);
    }
    public class AccountDeletionRequest
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public DateTime RequestDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CompletedDate { get; set; }
    }

    public class UserSubscriptionInfo
    {
        public bool IsActive { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal TotalAmountPaid { get; set; }
    }

    public class UserStatistics
    {
        public string Message;

        public int TotalUsers { get; set; }
        public int SubscribedUsers { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public object[] UserGrowthData { get; set; }
        public object[] RevenueData { get; set; }
        public object[] SubscriberDistribution { get; set; }
    }
    public class PaymentStatistics
    {
        public int TotalTransactions { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SuccessfulAmount { get; set; }
        public List<FailedTransaction> FailedTransactionDetails { get; set; }
        public List<PaymentSummary> DailyTransactions { get; set; }
    }

    public class FailedTransaction
    {
        public string CustomerRef { get; set; }
        public string PhoneNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string ResponseCode { get; set; }
    }

    public class PaymentSummary
    {
        public DateTime Date { get; set; }
        public int TotalCount { get; set; }
        public int SuccessfulCount { get; set; }
        public int FailedCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SuccessfulAmount { get; set; }
    }
}

