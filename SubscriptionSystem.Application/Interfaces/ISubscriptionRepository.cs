using SubscriptionSystem.Domain.Entities;
using System.Linq.Expressions;
namespace SubscriptionSystem.Application.Interfaces
{
    public interface ISubscriptionRepository

    {
        Task<Subscription> GetSubscriptionByCustomerRefAsync(string customerRef);

        Task<Subscription> GetActiveSubscriptionAsync(string userId);
        Task<Subscription> GetLatestSubscriptionAsync(string userId);
        Task AddAsync(Subscription subscription);
        Task UpdateSubscriptionAsync(Subscription subscription);
        Task<Subscription> GetSubscriptionByIdAsync(string subscriptionId);
        Task<IEnumerable<Subscription>> GetExpiredSubscriptionsAsync();
        Task<IEnumerable<Subscription>> GetSubscriptionHistoryAsync(string email);
        //Task<IEnumerable<Subscription>> GetExpiredSubscriptionsAsync(string email);
        Task<Subscription> GetByIdAsync(string id);
        Task<IEnumerable<Subscription>> GetAllAsync();
        Task UpdateAsync(Subscription subscription);
        Task DeleteAsync(string id);
        Task<bool> ActivateSubscriptionAsync(string userId);
        Task AddSubscriptionAsync(Subscription subscription);
        Task<IEnumerable<ExpiredSubscriptionEntity>> GetExpiredSubscriptionsAsync(string email);
        Task<bool> HasAnyActiveSubscriptionAsync();
        Task<IEnumerable<Subscription>> GetExpiringSubscriptionsAsync();
        Task<List<Subscription>> GetSubscriptionHistoryAsync(string userId, int pageNumber, int pageSize);
        Task<int> GetSubscriptionCountAsync(string userId);
        Task<IEnumerable<Subscription>> GetSubscriptionsExpiringSoonAsync(DateTime expiryDate);
        Task<Subscription> GetUserByCustomerRefAsync(string customerRef);
        Task<Subscription> AddOrUpdateSubscriptionAsync(Subscription subscription);
        Task<int> CountActiveSubscriptionsAsync();
        Task<int> CountAsync(Expression<Func<Subscription, bool>> predicate);
    }
}

