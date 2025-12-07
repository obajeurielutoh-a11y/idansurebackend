using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Infrastructure.Data;

namespace SubscriptionSystem.Infrastructure.Services
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly ApplicationDbContext _context;

        private readonly ILogger<ApiKeyService> _logger;

        public ApiKeyService(ApplicationDbContext context, ILogger<ApiKeyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User> GetUserByApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Attempted to get user with null or empty API key");
                throw new ArgumentNullException(nameof(apiKey));
            }

            try
            {
                _logger.LogInformation("Attempting to find user for API key: {ApiKey}", apiKey);

                var apiKeyEntity = await _context.ApiKeys
                    .Include(ak => ak.User)
                    .FirstOrDefaultAsync(ak => ak.Key == apiKey && ak.IsActive);

                if (apiKeyEntity == null)
                {
                    _logger.LogWarning("No active API key found for: {ApiKey}", apiKey);
                    return null;
                }

                if (apiKeyEntity.User == null)
                {
                    _logger.LogError("API key found but no associated user for: {ApiKey}", apiKey);
                    return null;
                }

                _logger.LogInformation("User found for API key: {ApiKey}, UserId: {UserId}", apiKey, apiKeyEntity.User.Id);
                return apiKeyEntity.User;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user by API key: {ApiKey}", apiKey);
                throw;
            }
        }

        public async Task<bool> ValidateApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return false;

            return await _context.ApiKeys
                .AnyAsync(u => u.Key == apiKey && u.IsActive);
        }

        public async Task<string> GenerateApiKeyForUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            string key = GenerateUniqueApiKey();
            var apiKey = new ApiKey
            {
                Key = key,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync();
            return key;
        }

        public async Task<bool> RevokeApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            var apiKeyEntity = await _context.ApiKeys
                .FirstOrDefaultAsync(u => u.Key == apiKey);

            if (apiKeyEntity == null)
                return false;

            apiKeyEntity.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateUniqueApiKey()
        {
            var key = new byte[32];
            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(key);
                string apiKey = Convert.ToBase64String(key);

                // Ensure the API key is unique
                while (_context.ApiKeys.Any(u => u.Key == apiKey))
                {
                    generator.GetBytes(key);
                    apiKey = Convert.ToBase64String(key);
                }

                return apiKey;
            }
        }
    }
}