using System;

namespace SubscriptionSystem.Domain.Entities
{
    public class ExpiredSubscriptionEntity
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string PlanType { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}

