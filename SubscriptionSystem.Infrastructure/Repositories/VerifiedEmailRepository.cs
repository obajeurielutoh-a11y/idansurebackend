using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace SubscriptionSystem.Infrastructure.Repositories
{
    public class VerifiedEmailRepository : IVerifiedEmailRepository
    {
        private readonly ApplicationDbContext _context;

        public VerifiedEmailRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsEmailVerifiedAsync(string email)
        {
            return await _context.VerifiedEmails.AnyAsync(ve => ve.Email == email);
        }

        public async Task AddVerifiedEmailAsync(string email)
        {
            if (!await IsEmailVerifiedAsync(email))
            {
                _context.VerifiedEmails.Add(new Domain.Entities.VerifiedEmail { Email = email });
                await _context.SaveChangesAsync();
            }
        }
    }
}

