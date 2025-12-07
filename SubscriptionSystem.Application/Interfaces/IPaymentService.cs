using SubscriptionSystem.Application.Common;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Domain.Entities.SubscriptionSystem.Domain.Entities;
namespace SubscriptionSystem.Application.Interfaces
{
    public interface IPaymentService
    {
        //Task<ServiceResult<StandardizedTransaction>> ProcessAlatPayWebhookAsync(PaymentWebhookDto webhookData);

        Task<ServiceResult<PaystackInitializeResponseDto>> InitializePaystackPaymentAsync(PaystackInitializeRequestDto request);
        Task<ServiceResult<PaystackVerifyResponseDto>> VerifyPaystackPaymentAsync(string reference);
        bool VerifyPaystackWebhookSignature(string payload, string signature);
        Task<Result<StandardizedTransaction>> ProcessUnifiedWebhookAsync(PaystackWebhookDto webhookData, string gateway);

        // CoralPay-specific webhook handler
        Task<Result<StandardizedTransaction>> ProcessCoralPayWebhookAsync(PaymentNotificationDto notification);
        Task<ServiceResult<bool>> ProcessPaymentWebhookAsync(PaymentWebhookDto webhookData);
        Task<ServiceResult<PaymentStatusDto>> GetPaymentStatusAsync(string userId);
        Task<ServiceResult<bool>> ProcessPaymentResponseAsync(PaymentResponseDto paymentResponse);
        Task<ServiceResult<bool>> VerifyEmailForSubscriptionAsync(string email);
        Task<ServiceResult<bool>> HasSuccessfulPaymentAsync(string email);
        Task<ServiceResult<bool>> InitiatePaymentAsync(decimal amount, string email);
        Task<ServiceResult<PaymentStatusDto>> QueryTransactionAsync(string traceId);
        Task<PaymentResponse> ProcessPaymentNotificationAsync(PaymentNotificationDto notification);
        //Task<NotificationResult<PaymentResponse>> ProcessPaymentNotificationAsync(PaymentNotificationDto notification);
        Task<NotificationResult<TransactionQueryResponseDto>> QueryTransactionDetailsAsync(string traceId);
        Task<Result<StandardizedTransaction>> ProcessUnifiedWebhookAsync(PaymentWebhookDto webhookData, string gateway);
        Task<Result<StandardizedTransaction>> ProcessUnifiedWebhookAsync(WebhookPayloadDto payload, string gatewayName);
        Task<Result<StandardizedTransaction>> ProcessUnifiedWebhookAsync(PaymentNotificationDto notification, string gateway);
        Task<ServiceResult<PaymentInitializationResponseDto>> InitializePaymentAsync(PaymentInitializationDto request);
        Task<ServiceResult<PaymentVerificationResponseDto>> VerifyPaymentAsync(string transRef);
        Task<ServiceResult<DirectCardChargeResponseDto>> InitiateDirectCardChargeAsync(DirectCardChargeDto request);
        Task<ServiceResult<AuthorizeCardChargeResponseDto>> AuthorizeDirectCardChargeAsync(AuthorizeCardChargeDto request);
        Task<ServiceResult<BankAccountValidationResponseDto>> ValidateBankAccountAsync(BankAccountValidationDto request);
        Task<ServiceResult<SettlementAccountResponseDto>> AddSettlementAccountAsync(SettlementAccountDto request);
        //Task<ServiceResult<bool>> ProcessWebhookAsync(WebhookPayloadDto payload);
        // New methods for unified transaction processing and reporting
      
        Task<Result<TransactionReportDto>> GetTransactionReportAsync(
            DateTime? startDate, DateTime? endDate, string gateway, string planType, int page, int pageSize);
        Task<Result<AdminDashboardDto>> GetAdminDashboardDataAsync();
        Task<Result<RevenueReportDto>> GetRevenueReportAsync(
            DateTime? startDate, DateTime? endDate, string groupBy,
        string status = null);
        Task ProcessUnifiedWebhookAsync(PaymentNotificationDto notification);
    }
}

