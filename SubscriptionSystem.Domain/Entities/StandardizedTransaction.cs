
namespace SubscriptionSystem.Domain.Entities
{
    namespace SubscriptionSystem.Domain.Entities
    {
        public class StandardizedTransaction
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string UserId { get; set; }
            public string Email { get; set; }
            public string PaymentGateway { get; set; } // "Credo", "AlatPay", "CoralPay"
            public string ExternalTransactionId { get; set; } // Reference from payment provider
            //public string InternalTransactionId { get; set; } // Our system's reference
            public decimal Amount { get; set; }
            public string Currency { get; set; } = "NGN";
            public string PlanType { get; set; } // "OneDay", "OneWeek", "OneMonth"
            public string Status { get; set; } // "Pending", "Completed", "Failed"
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime? CompletedAt { get; set; }
            public string RawPayload { get; set; } 
        }
    }
}
