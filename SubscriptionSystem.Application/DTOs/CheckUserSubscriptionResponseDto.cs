namespace SubscriptionSystem.Application.DTOs
{
    public class CheckUserSubscriptionResponseDto
    {
        public bool HasActiveSubscription { get; set; }
        public string Message { get; set; }
    }
}
