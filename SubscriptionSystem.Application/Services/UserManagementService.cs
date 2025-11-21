using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Application.DTOs;
using System;
using System.Threading.Tasks;
using System.Linq;
using SubscriptionSystem.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Configuration;
using OtpNet;
using Microsoft.EntityFrameworkCore;

namespace SubscriptionSystem.Application.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly string _uploadDirectory;

        public UserManagementService(IUserRepository userRepository, IEmailService emailService, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _uploadDirectory = configuration["UploadSettings:ProfilePictureDirectory"];
        }

        public async Task<PaymentStatistics> GetPaymentStatisticsAsync(DateTime startDate, DateTime? endDate = null)
        {
            if (endDate == null)
            {
                endDate = DateTime.Now;
            }

            // Check if any payment records exist
            bool anyPayments = await _userRepository.AnyPaymentRecordsAsync();
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
            var payments = await _userRepository.GetPaymentRecordsAsync(startDate, endDate.Value);

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
        public async Task<UserStatisticsDto> GetUserStatisticsAsync(string timeRange = "30d", string userType = "all")
        {
            // Calculate date range based on timeRange
            DateTime startDate = DateTime.UtcNow;
            switch (timeRange)
            {
                case "7d":
                    startDate = DateTime.UtcNow.AddDays(-7);
                    break;
                case "30d":
                    startDate = DateTime.UtcNow.AddDays(-30);
                    break;
                case "90d":
                    startDate = DateTime.UtcNow.AddDays(-90);
                    break;
                case "1y":
                    startDate = DateTime.UtcNow.AddYears(-1);
                    break;
                default:
                    startDate = DateTime.UtcNow.AddDays(-30); // Default to 30 days
                    break;
            }

            // Pass filters to repository
            var stats = await _userRepository.GetUserStatisticsAsync(startDate, userType);

            // Return filtered statistics
            return new UserStatisticsDto
            {
                TotalRegisteredUsers = stats.TotalUsers,
                TotalSubscribedUsers = stats.SubscribedUsers,
                TotalAmountPaidByUsers = stats.TotalAmountPaid,
                // Add additional data for charts
                UserGrowthData = stats.UserGrowthData,
                RevenueData = stats.RevenueData,
                SubscriberDistribution = stats.SubscriberDistribution
            };
        }
       
        

        public async Task UpdateUserAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user), "User cannot be null.");

            // Ensure you have a repository or database context to update the user
            await _userRepository.UpdateUserAsync(user);
        }

        public async Task<UserDto> GetUserByIdAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                IsActive = user.IsActive,
                Role = user.Role
                // Map other properties as needed
            };
        }
       
        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetUserByEmailAsync(email);
        }

        public async Task<PaginatedResult<UserDto>> GetUsersAsync(int page, int pageSize)
        {
            var (users, totalCount) = await _userRepository.GetUsersAsync(page, pageSize);
            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                
                IsActive = u.IsActive,
                Role = u.Role
            }).ToList();

            return new PaginatedResult<UserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<ServiceResult<string>> UploadProfilePictureAsync(string userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return new ServiceResult<string> { IsSuccess = false, ErrorMessage = "No file was uploaded." };
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<string> { IsSuccess = false, ErrorMessage = "User not found." };
            }

            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(_uploadDirectory, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            user.ProfilePictureUrl = $"/uploads/profile-pictures/{fileName}";
            await _userRepository.UpdateUserAsync(user);

            return new ServiceResult<string> { IsSuccess = true, Data = user.ProfilePictureUrl };
        }

        public async Task<ServiceResult<UserPreferencesDto>> UpdateUserPreferencesAsync(string userId, UserPreferencesDto preferences)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<UserPreferencesDto> { IsSuccess = false, ErrorMessage = "User not found." };
            }

 
            user.Email = preferences.Email;
            user.EnableNotifications = preferences.EnableNotifications;

            if (preferences.EnableTwoFactor && !user.TwoFactorEnabled)
            {
                user.TwoFactorEnabled = true;
                user.TwoFactorSecret = GenerateTwoFactorSecret();
                await _emailService.SendTwoFactorSetupEmailAsync(user.Email, user.TwoFactorSecret);
            }
            else if (!preferences.EnableTwoFactor && user.TwoFactorEnabled)
            {
                user.TwoFactorEnabled = false;
                user.TwoFactorSecret = null;
            }

            await _userRepository.UpdateUserAsync(user);

            return new ServiceResult<UserPreferencesDto>
            {
                IsSuccess = true,
                Data = new UserPreferencesDto
                {
                    
                    Email = user.Email,
                    EnableNotifications = user.EnableNotifications,
                    EnableTwoFactor = user.TwoFactorEnabled
                }
            };
        }

        public async Task<ServiceResult<UserPreferencesDto>> GetUserPreferencesAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<UserPreferencesDto> { IsSuccess = false, ErrorMessage = "User not found." };
            }

            var preferences = new UserPreferencesDto
            {
                
                Email = user.Email,
                EnableNotifications = user.EnableNotifications,
                EnableTwoFactor = user.TwoFactorEnabled
            };

            return new ServiceResult<UserPreferencesDto>
            {
                IsSuccess = true,
                Data = preferences
            };
        }

        private string GenerateTwoFactorSecret()
        {
            var secret = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(secret);
        }

       

        public async Task<ServiceResult<bool>> UpdateUserAsync(string userId, UpdateUserDto request)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "User not found." };
            }


            user.Role = request.Role;
            await _userRepository.UpdateUserAsync(user);

            await _emailService.SendEmailAsync(user.Email, "Account Updated", "Your account details have been updated by an administrator.");

            return new ServiceResult<bool> { IsSuccess = true, Data = true };
        }

        public async Task<ServiceResult<bool>> ToggleUserActivationAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "User not found." };
            }

            user.IsActive = !user.IsActive;
            await _userRepository.UpdateUserAsync(user);

            string subject = user.IsActive ? "Account Activated" : "Account Deactivated";
            string body = user.IsActive
                ? "Your account has been activated by an administrator."
                : "Your account has been deactivated by an administrator. Please contact support for more information.";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            return new ServiceResult<bool> { IsSuccess = true, Data = true };
        }

        public async Task<ServiceResult<UserPaymentStatusDto>> GetUserPaymentStatusAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<UserPaymentStatusDto> { IsSuccess = false, ErrorMessage = "User not found." };
            }

            var subscriptionInfo = await _userRepository.GetUserSubscriptionInfoAsync(userId);

            return new ServiceResult<UserPaymentStatusDto>
            {
                IsSuccess = true,
                Data = new UserPaymentStatusDto
                {
                    HasActiveSubscription = subscriptionInfo.IsActive,
                    SubscriptionExpiryDate = subscriptionInfo.ExpiryDate,
                    TotalAmountPaid = subscriptionInfo.TotalAmountPaid
                }
            };
        }

        public async Task<ServiceResult<bool>> ApproveAccountDeletionAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = $"User with ID {userId} not found."
                };
            }

            var deletionRequest = await _userRepository.GetAccountDeletionRequestAsync(userId);
            if (deletionRequest == null || !deletionRequest.IsActive)
            {
                return new ServiceResult<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = $"No active account deletion request found for user with ID {userId}."
                };
            }

            try
            {
                await _emailService.SendAccountDeletionApprovalNotificationAsync(user.Email);
                await _userRepository.DeleteUserAsync(userId);
                await _userRepository.MarkAccountDeletionRequestAsCompletedAsync(deletionRequest.Id);

                return new ServiceResult<bool> { IsSuccess = true, Data = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to approve account deletion: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<bool>> RequestAccountDeletionAsync(string userId, AccountDeletionRequestDto request)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "User ID cannot be null or empty." };
            }

            try
            {
                var userEmail = await GetUserEmailAsync(userId);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "User email not found." };
                }

                await _emailService.SendAccountDeletionRequestNotificationAsync(userId, userEmail);

                return new ServiceResult<bool> { IsSuccess = true, Data = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = $"Failed to process account deletion request: {ex.Message}" };
            }
        }
        public async Task<ServiceResult<List<AccountDeletionRequestDto>>> GetAllActiveAccountDeletionRequestsAsync()
        {
            try
            {
                var requests = await _userRepository.GetAllActiveAccountDeletionRequestsAsync();
                var requestDtos = requests.Select(r => new AccountDeletionRequestDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    RequestDate = r.RequestDate
                }).ToList();

                return new ServiceResult<List<AccountDeletionRequestDto>>
                {
                    IsSuccess = true,
                    Data = requestDtos
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<List<AccountDeletionRequestDto>>
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to retrieve active account deletion requests: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<bool>> DeleteUserAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "User not found." };
            }

            await _userRepository.DeleteUserAsync(userId);
            await _emailService.SendEmailAsync(user.Email, "Account Deleted", "Your account has been deleted by an administrator.");

            return new ServiceResult<bool> { IsSuccess = true, Data = true };
        }
        public async Task<string> GetUserEmailAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException($"User with ID {userId} not found.");
            }
            return user.Email;
        }



    }
}

