namespace SubscriptionSystem.Domain.Entities
{
    public class SubscriptionPlanDetails
    {
        public string PlanType { get; set; }
        public int Duration { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
    }
}

