using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Domain.Events
{
    public class SubscriptionActivatedEvent : DomainEvent
    {
        public string UserId { get; }
        public Guid SubscriptionId { get; }
    public SubscriptionPlan Plan { get; }
        public DateTime ExpiryDate { get; }
        public decimal AmountPaid { get; }

    public SubscriptionActivatedEvent(string userId, Guid subscriptionId, SubscriptionPlan plan, DateTime expiryDate, decimal amountPaid)
        {
            UserId = userId;
            SubscriptionId = subscriptionId;
            Plan = plan;
            ExpiryDate = expiryDate;
            AmountPaid = amountPaid;
        }
    }
}
