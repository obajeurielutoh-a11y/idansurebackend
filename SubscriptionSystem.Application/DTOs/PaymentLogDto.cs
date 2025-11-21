namespace SubscriptionSystem.Application.DTOs
{
    public class PaymentLogDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
