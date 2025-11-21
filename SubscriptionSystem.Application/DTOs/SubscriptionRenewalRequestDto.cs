namespace SubscriptionSystem.Application.DTOs
{
    public class SubscriptionRenewalRequestDto
    {
        public string SubscriptionId { get; set; }
        public decimal Amount { get; set; }     
        public string Email { get; set; }
        public double RenewalDays { get;  set; }
    }
}

