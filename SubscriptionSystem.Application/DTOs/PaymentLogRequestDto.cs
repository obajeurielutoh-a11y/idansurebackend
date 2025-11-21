namespace SubscriptionSystem.Application.DTOs
{
    public class PaymentLogRequestDto
    {
        public string Email { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string TransactionId { get; set; }
    }
}
