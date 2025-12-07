using System;

namespace SubscriptionSystem.Application.DTOs
{
    public class ExpiredSubscription
    {
        public string PlanType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Message { get; set; }
    }
}

