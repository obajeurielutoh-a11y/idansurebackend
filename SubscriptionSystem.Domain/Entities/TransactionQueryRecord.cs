namespace SubscriptionSystem.Domain.Entities
{
    public class TransactionQueryRecord
    {
        public int Id { get; set; }
        public string TraceId { get; set; }
        public string CustomerRef { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public DateTime TransactionDate { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public string PaymentReference { get; set; }
        public string TransactionStatus { get; set; }
    }
}
