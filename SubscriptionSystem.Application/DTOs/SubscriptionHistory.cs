using System;

namespace SubscriptionSystem.Application.DTOs
{
    public class SubscriptionHistory
    {
        public string PlanType { get; set; }
        public decimal AmountPaid { get; set; }
        public string Currency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Message { get; set; }
    }
}

