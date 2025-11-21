using System;

namespace SubscriptionSystem.Application.DTOs
{
    public class PaymentStatusDto
    {
        public bool HasActiveSubscription { get; set; }
        public DateTime? SubscriptionExpiryDate { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public string PaymentStatus { get; set; }
        public object LastPaymentStatus { get;  set; }
        // Add any other necessary properties
    }
}

