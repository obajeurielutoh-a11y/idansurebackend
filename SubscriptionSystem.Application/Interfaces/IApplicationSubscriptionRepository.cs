
using SubscriptionSystem.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IApplicationSubscriptionRepository
    {


        Task DeleteAsync(string id);
        Task<bool> ActivateSubscriptionAsync(string userId);

        Task<DateTime?> GetActiveSubscriptionByUserIdAsync(string userId);
        Task CreateSubscriptionAsync(Subscription subscription);
    }
}



