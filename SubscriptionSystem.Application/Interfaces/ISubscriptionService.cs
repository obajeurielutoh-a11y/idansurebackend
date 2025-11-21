using SubscriptionSystem.Application.Common;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Domain.Entities;


namespace SubscriptionSystem.Application.Interfaces
{
    public interface ISubscriptionService
    {
       
        Task<Result> HandleFailedPaymentAsync(FailedPaymentRequest request);
        SubscriptionPlanDetails GetSubscriptionPlanDetails(SubscriptionPlan plan);
   
        Task<ServiceResult<SubscriptionDto>> GetMostRecentExpiredSubscriptionAsync(string userId);

        //Task<Result> RenewSubscriptionAsync(SubscriptionRenewalDto request);
        Task<bool> HasActiveSubscriptionAsync(string userId);
        Task<bool> HasActiveSubscriptionByEmailAsync(string email);
        Task SendEmailToPremiumSubscribersAsync(string subject, string body);

        Task UpdateSubscriptionAsync(Subscription subscription);
        Task<ServiceResult<PaginatedList<SubscriptionHistoryDto>>> GetSubscriptionHistoryAsync(string email, int pageNumber, int pageSize);
        //Task<ServiceResult<bool>> RenewSubscriptionAsync(SubscriptionRenewalRequestDto request);
        Task<bool> HasAnyActiveSubscriptionAsync();

        Task<ServiceResult<SubscriptionDto>> PurchaseSubscriptionAsync(SubscriptionRequestDto request);
        Task<List<string>> GetPremiumSubscribersAsync();
       
        Task<ServiceResult<SubscriptionStatus>> GetSubscriptionStatusAsync(string email);

        Task<ServiceResult<bool>> NotifyExpiredSubscriptionsAsync();
        //Task<ServiceResult<List<SubscriptionHistoryDto>>> GetSubscriptionHistoryAsync(string email);
        Task<ServiceResult<List<ExpiredSubscriptionDto>>> GetExpiredSubscriptionsAsync(string email);
        Task<ServiceResult<SubscriptionDto>> AddAsync(Subscription subscription);
        Task<ServiceResult<SubscriptionDto>> GetActiveSubscriptionAsync(string userId);
        Task<ServiceResult<List<ExpiredSubscriptionDto>>> GetExpiredSubscriptionsAsync();
        Task<Subscription> GetSubscriptionByIdAsync(string id);
        Task<ServiceResult<bool>> ProcessPaymentAsync(PaymentRequestDto paymentRequest);
        Task<ServiceResult<bool>> NotifyExpiringSubscriptionsAsync();
        
        // Handle i-Cell DataSync notification
        Task HandleICellDataSyncAsync(string msisdn, string? productId, string? errorCode, string? errorMsg);
      }
}
