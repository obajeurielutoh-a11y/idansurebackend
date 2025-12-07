namespace SubscriptionSystem.Application.DTOs
{
    public class UserPaymentStatusDto
    {
        public bool HasActiveSubscription { get; set; }
        public DateTime? SubscriptionExpiryDate { get; set; }
        public decimal TotalAmountPaid { get; set; }
    }
}

