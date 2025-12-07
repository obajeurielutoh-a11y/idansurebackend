using System;

namespace SubscriptionSystem.Application.DTOs
{
    public class SubscriptionStatus
    {
        public bool IsActive { get; set; }
        public string PlanType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int RemainingDays { get; set; }
        public string Message { get; set; }


    }
}

