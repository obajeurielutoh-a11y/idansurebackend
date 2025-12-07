namespace SubscriptionSystem.Application.DTOs
{
    public class UserSubscriptionResponseDto
    {
        public string TraceId { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public string DisplayMessage { get; set; }
        public string ResponseCode { get; set; }
    }
}
