using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Application.DTOs
{
    // DTO for subscription status (Active or Expired)
   

    // DTO for failed payment requests
    public class FailedPaymentRequest
    {
        public string TransactionId { get; set; }
        public string Email { get; set; }
        public string Reason { get; set; }
    }

    // DTO for subscription plan details
    public class SubscriptionPlanDetail
    {
        public SubscriptionPlan Plan { get; set; }
        public string Duration { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
    }

    public class Result
    {
        public bool IsSuccess { get; set; }
        public bool ErrorMessage { get; set; }
        public string Message { get; set; }
        public string Failure { get; set; }
    }
    public class SubscriptionRequestDto
    {
        public string Email { get; set; }
        public string PlanType { get; set; }
        public decimal AmountPaid { get; set; }
        public string Currency { get; set; }
        public string PhoneNumber { get; set; }
        public string TransactionId { get; set; }


    }

    public class SubscriptionRenewalDto
    {
        public string Email { get; set; }
        public string TransactionId { get; set; }
        public double RenewalDays { get; set; }
        public string UserId { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
