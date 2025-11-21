namespace SubscriptionSystem.Application.DTOs
{
    public class PaymentNotificationDto
    {
        public string PassBackReference { get; set; }
        public string TraceId { get; set; }
        public string PaymentReference { get; set; }
        public string CustomerRef { get; set; }
    
        public string ResponseCode { get; set; }
        public string MerchantId { get; set; }
        public string MobileNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string ShortCode { get; set; }
        public string Currency { get; set; }
        public string Channel { get; set; }
        public string Hash { get; set; }
       
    }
    
}
