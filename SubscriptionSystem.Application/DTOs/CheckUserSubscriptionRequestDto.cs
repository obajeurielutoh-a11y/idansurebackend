namespace SubscriptionSystem.Application.DTOs
{
    public class CheckUserSubscriptionRequestDto
    {
        public string CustomerRef { get; set; } // Changed from PhoneNumber to CustomerRef
        public string MerchantId { get; set; }
        public string ShortCode { get; set; }
    }
}
