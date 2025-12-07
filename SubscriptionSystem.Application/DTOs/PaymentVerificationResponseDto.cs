namespace SubscriptionSystem.Application.DTOs
{
    public class PaymentVerificationResponseDto
    {
        public string TransRef { get; set; }
        public string BusinessRef { get; set; }
        public decimal DebitedAmount { get; set; }
        public decimal TransAmount { get; set; }
        public decimal TransFeeAmount { get; set; }
        public decimal SettlementAmount { get; set; }
        public string CustomerId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string CurrencyCode { get; set; }
        public int Status { get; set; }
        public List<MetadataItem> Metadata { get; set; }
    }

    public class MetadataItem
    {
        public string InsightTag { get; set; }
        public string InsightTagValue { get; set; }
        public string InsightTagDisplay { get; set; }
    }
}