using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IVerifiedEmailRepository
    {
        Task<bool> IsEmailVerifiedAsync(string email);
        Task AddVerifiedEmailAsync(string email);
    }
}

