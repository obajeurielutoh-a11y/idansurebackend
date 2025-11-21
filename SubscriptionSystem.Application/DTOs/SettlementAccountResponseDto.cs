namespace SubscriptionSystem.Application.DTOs
{
    public class SettlementAccountResponseDto
    {
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string BankName { get; set; }
        public string BankCode { get; set; }
        public string BankLogo { get; set; }
        public string CurrencyCode { get; set; }
        public string AccountType { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
