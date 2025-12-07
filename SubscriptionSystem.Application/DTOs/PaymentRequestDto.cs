using System;

namespace SubscriptionSystem.Application.DTOs
{
    public class PaymentRequestDto
    {
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime PaymentDate { get; set; }
        public string TransactionId { get; set; }
        public string Email { get; set; }
        public string Reason { get; set; }
    }
}

