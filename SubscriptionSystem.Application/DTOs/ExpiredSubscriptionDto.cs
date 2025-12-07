using System;

namespace SubscriptionSystem.Application.DTOs
{
    public class ExpiredSubscriptionDto
    {
        public string PlanType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Message { get; set; }
    }
}