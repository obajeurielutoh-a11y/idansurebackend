namespace SubscriptionSystem.Application.DTOs
{
    public class SubscriptionDto
    {
        public Guid Id { get; set; }
       
        public string PlanType { get; set; }
        public decimal AmountPaid { get; set; }
        public string Currency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public string UserId { get; set; }

        
    }
}



