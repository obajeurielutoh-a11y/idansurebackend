namespace SubscriptionSystem.Application.DTOs
{
    public class SubscriptionExtendRequestDto
    {
        public string Email { get; set; }
        public int RenewalDays { get; set; } // Number of days to extend the subscription
    }
}
