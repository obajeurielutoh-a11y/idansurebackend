using Microsoft.AspNetCore.Http;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Domain.Entities;
using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IUserManagementService
    {
        Task<UserDto> GetUserByIdAsync(string userId);
   
        Task<PaginatedResult<UserDto>> GetUsersAsync(int page, int pageSize);
        Task<ServiceResult<bool>> UpdateUserAsync(string userId, UpdateUserDto request);
        Task<ServiceResult<bool>> ToggleUserActivationAsync(string userId);
        Task<ServiceResult<UserPaymentStatusDto>> GetUserPaymentStatusAsync(string userId);
        Task<ServiceResult<bool>> ApproveAccountDeletionAsync(string userId);
        //Task<UserStatisticsDto> GetUserStatisticsAsync();
        Task<ServiceResult<bool>> DeleteUserAsync(string userId);
        Task<ServiceResult<string>> UploadProfilePictureAsync(string userId, IFormFile file);
        Task<ServiceResult<bool>> RequestAccountDeletionAsync(string userId, AccountDeletionRequestDto request);
        Task<ServiceResult<UserPreferencesDto>> UpdateUserPreferencesAsync(string userId, UserPreferencesDto preferences);
        Task<ServiceResult<UserPreferencesDto>> GetUserPreferencesAsync(string userId);
        Task<string> GetUserEmailAsync(string userId);
        Task<User> GetUserByEmailAsync(string email);
        // Update this method signature to match the implementation
        Task<UserStatisticsDto> GetUserStatisticsAsync(string timeRange = "30d", string userType = "all");
        Task UpdateUserAsync(User user);
        Task<PaymentStatistics> GetPaymentStatisticsAsync(DateTime start, DateTime? endDate = null);
        Task<ServiceResult<List<AccountDeletionRequestDto>>> GetAllActiveAccountDeletionRequestsAsync();
        //Task<PaymentStatistics> GetPaymentStatisticsAsync(DateTime start, DateTime? endDate);
    }
}

