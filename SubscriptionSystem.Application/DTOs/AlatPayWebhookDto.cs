using System;

namespace SubscriptionSystem.Application.DTOs
{
    public class AlatPayWebhookDto
    {
        public WebhookValue Value { get; set; }
        public int StatusCode { get; set; }

    }


    public class AlatPayWebhook
    {
        public WebhookData Data { get; set; }
        public bool Status { get; set; }
        public string Message { get; set; }
    }

    public class AlatPayWebhookData
    {
        public decimal Amount { get; set; }
        public string? OrderId { get; set; }
        public string? Description { get; set; }
        public int PaymentMethodId { get; set; }
        public string? SessionId { get; set; }
        public CustomerInfo Customer { get; set; }
        public string Id { get; set; }
        public string? MerchantId { get; set; }
        public string? BusinessId { get; set; }
        public string Channel { get; set; }
        public string? CallbackUrl { get; set; }
        public decimal FeeAmount { get; set; }
        public string PlanType { get; set; }
        public string? BusinessName { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string? StatusReason { get; set; }
        public string? SettlementType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? NgnVirtualBankAccountNumber { get; set; }
        public string? UsdVirtualAccountNumber { get; set; }

    }

    public class CustomerInfo
    {
        public string Id { get; set; }
        public string TransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

    }
}

