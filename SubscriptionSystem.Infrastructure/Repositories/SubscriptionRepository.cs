using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Application.Interfaces;
using System.Linq.Expressions;


namespace SubscriptionSystem.Infrastructure.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Subscription> AddOrUpdateSubscriptionAsync(Subscription subscription)
        {
            // Check if user already has an active subscription
            var existingSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == subscription.UserId && s.IsActive);

            if (existingSubscription != null)
            {
                // Update existing subscription
                existingSubscription.PlanType = subscription.PlanType;
                existingSubscription.AmountPaid = subscription.AmountPaid;
                existingSubscription.PaymentGateway = subscription.PaymentGateway;
                existingSubscription.TransactionId = subscription.TransactionId;

                // If the new subscription extends beyond the current end date, update it
                if (subscription.ExpiryDate > existingSubscription.ExpiryDate)
                {
                    existingSubscription.ExpiryDate = subscription.ExpiryDate;
                }

                // Update the last updated timestamp
                existingSubscription.LastUpdated = DateTime.UtcNow;

                _context.Subscriptions.Update(existingSubscription);
                await _context.SaveChangesAsync();

                return existingSubscription;
            }
            else
            {
                // Add new subscription
                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                return subscription;
            }
        }

        public async Task<int> CountActiveSubscriptionsAsync()
        {
            return await _context.Subscriptions.CountAsync();
        }


        //public async Task<int> CountAsync(Expression<Func<Subscription, bool>> predicate)
        //{
        //    return await _context.Subscriptions.CountAsync(predicate);
        //}

        public async Task<int> CountAsync(Expression<Func<Subscription, bool>> predicate)
        {
            return await _context.Subscriptions
                .Where(predicate)
                .CountAsync();
        }

        public async Task<IEnumerable<Subscription>> GetAllAsync()
        {
            return await _context.Subscriptions.ToListAsync();
        }
        public async Task<Subscription> GetSubscriptionByCustomerRefAsync(string customerRef)
        {
            return await _context.Subscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.PhoneNumber == customerRef);
        }



        public async Task<Subscription> GetByIdAsync(string id)
        {
            return await _context.Subscriptions.FindAsync(id);
        }

        public async Task<User> GetUserByCustomerRefAsync(string customerRef)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == customerRef);
        }

        public async Task<Subscription> GetActiveSubscriptionAsync(string userId)
        {
            return await _context.Subscriptions
                .Where(s => s.UserId == userId && s.IsActive && s.ExpiryDate > DateTime.UtcNow)
                .OrderByDescending(s => s.ExpiryDate) // Pick the most recent
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Subscription>> GetSubscriptionsExpiringSoonAsync(DateTime expiryDate)
        {
            return await _context.Subscriptions
                .Where(s => s.ExpiryDate <= expiryDate && s.IsActive)
                .ToListAsync();
        }




        public async Task<IEnumerable<Subscription>> GetSubscriptionHistoryAsync(string email)
        {
            return (IEnumerable<Subscription>)await _context.Subscriptions
                .Include(s => s.User)
                .Where(s => s.User.Email == email)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
        }


        public async Task<IEnumerable<Subscription>> GetExpiringSubscriptionsAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Subscriptions
                .Where(s => s.IsActive &&
                    (s.Plan == SubscriptionPlan.OneDay && s.ExpiryDate < now.AddHours(6)) ||
                    (s.Plan != SubscriptionPlan.OneDay && s.ExpiryDate < now.AddDays(3)))
                .ToListAsync();
        }

        public async Task AddAsync(Subscription subscription)
        {
            await _context.Subscriptions.AddAsync(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSubscriptionAsync(Subscription subscription)
        {
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }
        public async Task<List<Subscription>> GetSubscriptionHistoryAsync(string userId, int pageNumber, int pageSize)
        {
            return await _context.Subscriptions
                .Include(s => s.User)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetSubscriptionCountAsync(string userId)
        {
            return await _context.Subscriptions
                .Where(s => s.UserId == userId)
                .CountAsync();
        }

       public async Task<Subscription> GetSubscriptionByIdAsync(string subscriptionId)
        {
            return await _context.Subscriptions.FindAsync(subscriptionId);
        }


        

        public async Task<Subscription> GetLatestSubscriptionAsync(string userId)
        {
            return await _context.Subscriptions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();
        }
        public async Task UpdateAsync(Subscription subscription)
        {
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription != null)
            {
                _context.Subscriptions.Remove(subscription);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ActivateSubscriptionAsync(string userId)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (subscription == null)
            {
                subscription = new Subscription
                {
                    UserId = userId,
                    StartDate = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddMonths(1),
                    IsActive = true
                };
                _context.Subscriptions.Add(subscription);
            }
            else
            {
                subscription.IsActive = true;
                subscription.StartDate = DateTime.UtcNow;
                subscription.ExpiryDate = DateTime.UtcNow.AddMonths(1);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasAnyActiveSubscriptionAsync()
        {
            try
            {
                var hasActiveSubscription = await _context.Subscriptions
                    .AnyAsync(s => s.IsActive && s.ExpiryDate > DateTime.UtcNow);

               
                return hasActiveSubscription;
            }
            catch (Exception ex)
            {
               
                throw;
            }
        }


        public async Task AddSubscriptionAsync(Subscription subscription)
        {
            // Add the new subscription to the DbContext
            await _context.Subscriptions.AddAsync(subscription);
            // Save changes to the database
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<ExpiredSubscriptionEntity>> GetExpiredSubscriptionsAsync(string email)
        {
            return await _context.Subscriptions
                .Include(s => s.User)
                .Where(s => s.User.Email == email && s.ExpiryDate < DateTime.UtcNow)
                .Select(s => new ExpiredSubscriptionEntity
                {
                    Id = s.UserId,
                    UserId = s.UserId,
                    PlanType = s.PlanType,
                    ExpiryDate = s.ExpiryDate
                })
                .OrderByDescending(s => s.ExpiryDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subscription>> GetExpiredSubscriptionsAsync()
        {
            return await _context.Subscriptions
                .Where(s => s.ExpiryDate < DateTime.UtcNow && s.IsActive)
                .ToListAsync();
        }

        Task<Subscription> ISubscriptionRepository.GetUserByCustomerRefAsync(string customerRef)
        {
            throw new NotImplementedException();
        }
    }
}

