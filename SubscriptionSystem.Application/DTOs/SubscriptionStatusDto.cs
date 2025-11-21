namespace SubscriptionSystem.Application.DTOs
{
    public class SubscriptionStatusDto
    {
      

        public bool IsActive { get; internal set; }
        public string Message { get; internal set; }
        public string PlanType { get; internal set; }
        public DateTime ExpiryDate { get; internal set; }
        public int RemainingDays { get; internal set; }
    }
}
