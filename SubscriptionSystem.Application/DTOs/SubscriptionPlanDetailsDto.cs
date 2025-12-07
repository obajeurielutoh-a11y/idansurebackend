namespace SubscriptionSystem.Application.DTOs
{
    public class SubscriptionPlanDetailsDto
    {
        public string PlanType { get; internal set; }
        public int Duration { get; internal set; }
        public decimal Price { get; internal set; }
        public string Description { get;  set; }


    }
}
