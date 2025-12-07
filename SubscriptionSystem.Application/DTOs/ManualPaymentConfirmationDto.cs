namespace SubscriptionSystem.Application.DTOs
{
    public class ManualPaymentConfirmationDto
    {
        public string TransactionId { get; set; }
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Currency { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        // Add any other necessary properties
    }
}

