namespace SubscriptionSystem.Application.DTOs
{
    public class PaymentInitializationResponseDto
    {
        public string AuthorizationUrl { get; set; }
        public string Reference { get; set; }
        public string CredoReference { get; set; }
        public string Crn { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal Fee { get; set; }
        public decimal Amount { get; set; }
    }
}
