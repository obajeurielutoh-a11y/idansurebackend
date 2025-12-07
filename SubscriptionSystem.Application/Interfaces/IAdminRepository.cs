using SubscriptionSystem.Domain.Entities;
using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IAdminRepository
    {
        Task<Admin> GetAdminByIdAsync(string id);
        Task<Admin> GetAdminByEmailAsync(string email);
        Task CreateAdminAsync(Admin admin);
        Task UpdateAdminAsync(Admin admin);
    }
}

